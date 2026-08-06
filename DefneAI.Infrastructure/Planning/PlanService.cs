using System.Text;
using System.Text.Json;
using DefneAI.Application.DTOs;
using DefneAI.Application.InitializerService;
using DefneAI.Application.Planning;
using DefneAI.Application.PromptFilter;
using DefneAI.Application.PromptStrategy;
using DefneAI.Application.Repository;
using DefneAI.Domain.Enums;
using DefneAI.Domain.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Infrastructure.Planning;

public sealed class PlanService(
    IModelInitializerService modelInitializerService,
    PromptFilterPipeline promptFilterPipeline,
    IEnumerable<IPromptStrategy> promptStrategies,
    IPromptRepository promptRepository) : IPlanService
{
    private const int FullReplanRetryThreshold = 5;

    private readonly IReadOnlyList<IPromptStrategy> registeredStrategies =
        promptStrategies.ToArray();

    public async Task<string> ExecutePlanAsync(
        Prompt prompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default)
    {
        Validate(prompt, chatHistoryThread, cancellationToken);

        PlanDto plan = await CreatePlanAsync(
            prompt,
            chatHistoryThread,
            cancellationToken);
        string? latestResponse = null;

        while (plan.Steps.Length > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string currentStep = plan.Steps[0];
            plan.Steps = plan.Steps[1..];
            Prompt stepPrompt = CreateStepPrompt(prompt, currentStep);

            try
            {
                await promptFilterPipeline.ControlAsync(
                    stepPrompt,
                    cancellationToken);
                AITaskType stepIntent = stepPrompt.PromptIntent
                    ?? throw new InvalidOperationException(
                        "Plan step intent has not been assigned.");
                IPromptStrategy promptStrategy = GetPromptStrategy(stepIntent);

                stepPrompt.State = PromptState.Executing;
                latestResponse = await promptStrategy.ExecutionAsync(
                    stepPrompt,
                    chatHistoryThread,
                    cancellationToken);
                if (string.IsNullOrWhiteSpace(latestResponse))
                {
                    throw new InvalidOperationException(
                        "The plan step produced no response.");
                }

                stepPrompt.State = PromptState.Completed;
                plan.RetryCount = 0;
                plan.LastFailedStep = string.Empty;
                plan.FailureReason = string.Empty;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                stepPrompt.State = PromptState.Failed;
                plan.LastFailedStep = currentStep;
                plan.FailureReason = exception.Message;
                plan.RetryCount++;
            }

            if (stepPrompt.State != PromptState.Failed)
            {
                continue;
            }

            prompt.State = PromptState.Failed;
            await promptRepository.UpdateAsync(prompt, cancellationToken);

            if (plan.RetryCount >= FullReplanRetryThreshold)
            {
                plan = await CreatePlanAsync(
                    prompt,
                    chatHistoryThread,
                    cancellationToken);
            }
            else
            {
                await RebuildPlanAsync(
                    prompt,
                    plan,
                    chatHistoryThread,
                    cancellationToken);
            }

            prompt.State = PromptState.Executing;
            await promptRepository.UpdateAsync(prompt, cancellationToken);
        }

        return latestResponse
            ?? throw new InvalidOperationException(
                "The execution plan produced no response.");
    }

    public async Task<PlanDto> CreatePlanAsync(
        Prompt prompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default)
    {
        Validate(prompt, chatHistoryThread, cancellationToken);

        string planningPrompt = $"""
            Create an execution plan for the user request below.
            Return only a JSON array of strings. Do not return markdown or an object.
            Each string must be one concrete, self-contained step that can be sent
            to an AI agent as a normal prompt. Keep the original request's details
            in the steps that need them. Use one step when decomposition is not
            necessary. The final step must produce the response for the user.

            User request:
            {prompt.Content}
            """;

        return new PlanDto
        {
            Steps = await CreateStepsAsync(
                prompt,
                planningPrompt,
                chatHistoryThread,
                cancellationToken)
        };
    }

    public async Task RebuildPlanAsync(
        Prompt prompt,
        PlanDto plan,
        ChatHistoryAgentThread chatHistoryAgentThread,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(plan.LastFailedStep);
        ArgumentException.ThrowIfNullOrWhiteSpace(plan.FailureReason);
        Validate(prompt, chatHistoryAgentThread, cancellationToken);

        string remainingSteps = plan.Steps.Length == 0
            ? "No unexecuted steps remain."
            : string.Join(
                Environment.NewLine,
                plan.Steps.Select((step, index) => $"{index + 1}. {step}"));
        string replanningPrompt = $"""
            Rebuild only the unfinished part of the execution plan after a failed step.
            Return only a JSON array of strings. Do not return markdown or an object.
            Do not repeat steps that completed before the failure. Correct or replace
            the failed step, then preserve or adjust the still-unexecuted steps as
            needed. Every returned string must be a concrete, self-contained prompt.
            The final step must produce the response for the user.

            Original user request:
            {prompt.Content}

            Failed step:
            {plan.LastFailedStep}

            Failure reason:
            {plan.FailureReason}

            Retry count:
            {plan.RetryCount}

            Still-unexecuted steps:
            {remainingSteps}
            """;

        plan.Steps = await CreateStepsAsync(
            prompt,
            replanningPrompt,
            chatHistoryAgentThread,
            cancellationToken);
    }

    private async Task<string[]> CreateStepsAsync(
        Prompt prompt,
        string planningPrompt,
        ChatHistoryAgentThread chatHistoryAgentThread,
        CancellationToken cancellationToken)
    {
        IList<ChatCompletionAgent> agents =
            await modelInitializerService.GetChatCompletionAgentsAsync(
                prompt.PromptIntent!.Value);
        if (agents.Count == 0)
        {
            throw new InvalidOperationException(
                "A plan cannot be created because no AI model is available.");
        }

        int agentIndex = Math.Min((int)prompt.PromptLevel!.Value, agents.Count - 1);
        ChatCompletionAgent planningAgent = agents[agentIndex];
        StringBuilder responseBuilder = new();

        await foreach (AgentResponseItem<ChatMessageContent> response in
            planningAgent.InvokeAsync(
                planningPrompt,
                thread: chatHistoryAgentThread,
                cancellationToken: cancellationToken))
        {
            responseBuilder.Append(response.Message.Content);
        }

        return ParseSteps(responseBuilder.ToString());
    }

    private static string[] ParseSteps(string response)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(response);

        int arrayStart = response.IndexOf('[');
        int arrayEnd = response.LastIndexOf(']');
        if (arrayStart < 0 || arrayEnd <= arrayStart)
        {
            throw new InvalidOperationException(
                "The planning model did not return a JSON string array.");
        }

        try
        {
            string[]? parsedSteps = JsonSerializer.Deserialize<string[]>(
                response[arrayStart..(arrayEnd + 1)]);
            string[] steps = parsedSteps?
                .Where(step => !string.IsNullOrWhiteSpace(step))
                .Select(step => step.Trim())
                .ToArray() ?? [];

            return steps.Length > 0
                ? steps
                : throw new InvalidOperationException(
                    "The planning model returned an empty plan.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "The planning model returned an invalid JSON string array.",
                exception);
        }
    }

    private static Prompt CreateStepPrompt(Prompt source, string step)
    {
        return new Prompt
        {
            Id = source.Id,
            ChatId = source.ChatId,
            Content = step,
            CreatedAtUtc = source.CreatedAtUtc
        };
    }

    private IPromptStrategy GetPromptStrategy(AITaskType promptIntent)
    {
        return promptIntent switch
        {
            AITaskType.Coding => registeredStrategies.Single(
                strategy => strategy.Intent == AITaskType.Coding),
            AITaskType.OfficeTask => registeredStrategies.Single(
                strategy => strategy.Intent == AITaskType.OfficeTask),
            AITaskType.WebSearch => registeredStrategies.Single(
                strategy => strategy.Intent == AITaskType.WebSearch),
            AITaskType.GeneralChat => registeredStrategies.Single(
                strategy => strategy.Intent == AITaskType.GeneralChat),
            _ => throw new InvalidOperationException(
                $"Unsupported prompt intent: {promptIntent}.")
        };
    }

    private static void Validate(
        Prompt prompt,
        ChatHistoryAgentThread chatHistoryAgentThread,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt.Content);
        ArgumentNullException.ThrowIfNull(chatHistoryAgentThread);
        cancellationToken.ThrowIfCancellationRequested();

        if (prompt.PromptIntent is null || prompt.PromptLevel is null)
        {
            throw new InvalidOperationException(
                "Prompt analysis must be completed before plan creation.");
        }
    }
}
