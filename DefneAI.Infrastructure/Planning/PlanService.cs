using System.Text;
using System.Text.Json;
using DefneAI.Application.DTOs;
using DefneAI.Application.Helpers;
using DefneAI.Application.InitializerService;
using DefneAI.Application.Planning;
using DefneAI.Domain.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Infrastructure.Planning;

public sealed class PlanService(
    IModelInitializerService modelInitializerService) : IPlanService
{
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

        PlanDto plan = new();
        plan.Steps.AddRange(await CreateStepsAsync(
            prompt,
            planningPrompt,
            chatHistoryThread,
            cancellationToken));
        return plan;
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

        string remainingSteps = plan.Steps.Count == 0
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

        IReadOnlyList<string> rebuiltSteps = await CreateStepsAsync(
            prompt,
            replanningPrompt,
            chatHistoryAgentThread,
            cancellationToken);

        plan.Steps.Clear();
        plan.Steps.AddRange(rebuiltSteps);
    }

    private async Task<IReadOnlyList<string>> CreateStepsAsync(
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
        ChatHistoryAgentThread planningThread =
            ChatHistoryThreadFactory.CreateCopy(chatHistoryAgentThread);
        StringBuilder responseBuilder = new();

        await foreach (AgentResponseItem<ChatMessageContent> response in
            planningAgent.InvokeAsync(
                planningPrompt,
                thread: planningThread,
                cancellationToken: cancellationToken))
        {
            responseBuilder.Append(response.Message.Content);
        }

        string modelResponse = responseBuilder.ToString();
        ArgumentException.ThrowIfNullOrWhiteSpace(modelResponse);

        int arrayStart = modelResponse.IndexOf('[');
        int arrayEnd = modelResponse.LastIndexOf(']');
        if (arrayStart < 0 || arrayEnd <= arrayStart)
        {
            throw new InvalidOperationException(
                "The planning model did not return a JSON string array.");
        }

        try
        {
            List<string>? parsedSteps = JsonSerializer.Deserialize<List<string>>(
                modelResponse[arrayStart..(arrayEnd + 1)]);
            List<string> steps = parsedSteps?
                .Where(step => !string.IsNullOrWhiteSpace(step))
                .Select(step => step.Trim())
                .ToList() ?? [];

            return steps.Count > 0
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
