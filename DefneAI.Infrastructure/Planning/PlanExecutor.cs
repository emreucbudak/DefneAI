using DefneAI.Application.DTOs;
using DefneAI.Application.Planning;
using DefneAI.Application.PromptAnalysis;
using DefneAI.Application.PromptStrategy;
using DefneAI.Domain.Enums;
using DefneAI.Domain.Models;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Infrastructure.Planning;

public sealed class PlanExecutor(
    IPlanService planService,
    IPromptAnalysisService promptAnalysisService,
    IEnumerable<IPromptStrategy> promptStrategies) : IPlanExecutor
{
    private const int FullReplanRetryThreshold = 5;

    private readonly IReadOnlyList<IPromptStrategy> registeredStrategies =
        promptStrategies.ToArray();

    public async Task<string> ExecuteAsync(
        Prompt prompt,
        ChatHistoryAgentThread executionThread,
        CancellationToken cancellationToken = default)
    {
        Validate(prompt, executionThread, cancellationToken);

        PlanDto plan = await planService.CreatePlanAsync(
            prompt,
            executionThread,
            cancellationToken);
        string? latestResponse = null;

        while (plan.Steps.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string currentStep = plan.Steps[0];
            plan.Steps.RemoveAt(0);
            bool isFinalStep = plan.Steps.Count == 0;
            Prompt stepPrompt = CreateStepPrompt(prompt, currentStep);

            try
            {
                PromptAnalysisResult stepAnalysis =
                    await promptAnalysisService.AnalyzeAsync(
                        stepPrompt,
                        cancellationToken);
                IPromptStrategy promptStrategy =
                    registeredStrategies.Single(
                        strategy => strategy.Intent == stepAnalysis.Intent);
                stepPrompt.State = PromptState.Executing;
                latestResponse = await promptStrategy.ExecutionAsync(
                    stepPrompt,
                    executionThread,
                    cancellationToken,
                    persistResponse: isFinalStep);
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

            if (plan.RetryCount >= FullReplanRetryThreshold)
            {
                plan = await planService.CreatePlanAsync(
                    prompt,
                    executionThread,
                    cancellationToken);
            }
            else
            {
                await planService.RebuildPlanAsync(
                    prompt,
                    plan,
                    executionThread,
                    cancellationToken);
            }
        }

        return latestResponse
            ?? throw new InvalidOperationException(
                "The execution plan produced no response.");
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

    private static void Validate(
        Prompt prompt,
        ChatHistoryAgentThread executionThread,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt.Content);
        ArgumentNullException.ThrowIfNull(executionThread);
        cancellationToken.ThrowIfCancellationRequested();

        if (prompt.PromptIntent is null || prompt.PromptLevel is null)
        {
            throw new InvalidOperationException(
                "Prompt analysis must be completed before plan execution.");
        }
    }
}
