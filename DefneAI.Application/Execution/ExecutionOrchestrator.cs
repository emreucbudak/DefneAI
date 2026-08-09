using DefneAI.Application.Helpers;
using DefneAI.Application.Planning;
using DefneAI.Application.PromptAnalysis;
using DefneAI.Application.PromptStates;
using DefneAI.Application.PromptStrategy;
using DefneAI.Application.Repository;
using DefneAI.Domain.Enums;
using DefneAI.Domain.Models;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.Execution;

public sealed class  ExecutionOrchestrator(
    IPromptAnalysisService promptAnalysisService,
    IEnumerable<IPromptStrategy> promptStrategies,
    IPlanExecutor planExecutor,
    IPromptRepository promptRepository,
    IContext context) : IExecutionOrchestrator
{
    private readonly IReadOnlyList<IPromptStrategy> registeredStrategies =
        promptStrategies.ToArray();

    public async Task<string> ExecuteAsync(
        ExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request, cancellationToken);

        Prompt prompt = Prompt.Create(request.ChatId, request.Content);
        await promptRepository.AddAsync(prompt, cancellationToken);

        try
        {
            context.State.TransitionTo(context, PromptState.Thinking);
            await context.State.WriteAsync(async () =>
            {
                await promptAnalysisService.AnalyzeAsync(
                    prompt,
                    request.ChatHistoryThread,
                    cancellationToken);
            });

            context.State.TransitionTo(context, PromptState.Executing);
            prompt.StartExecution();

            ChatHistoryAgentThread executionThread =
                ChatHistoryThreadFactory.CreateCopy(request.ChatHistoryThread);
            string? response = null;
            await context.State.WriteAsync(async () =>
            {
                response = prompt.ExecutionMode switch
                {
                    ExecutionMode.Direct => await ExecuteDirectAsync(
                        prompt,
                        executionThread,
                        cancellationToken),
                    ExecutionMode.Planned => await planExecutor.ExecuteAsync(
                        prompt,
                        executionThread,
                        cancellationToken),
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(prompt.ExecutionMode),
                        prompt.ExecutionMode,
                        "Unsupported execution mode.")
                };
            });

            if (string.IsNullOrWhiteSpace(response))
            {
                throw new InvalidOperationException(
                    "Prompt execution returned no response.");
            }

            ChatHistoryThreadFactory.AppendExchange(
                request.ChatHistoryThread,
                request.Content,
                response);

            context.State.TransitionTo(context, PromptState.Completed);
            prompt.Complete();
            await promptRepository.SaveAsync(prompt, cancellationToken);
            await context.State.WriteAsync();

            return response;
        }
        catch
        {
            context.State.TransitionTo(context, PromptState.Failed);
            prompt.Fail();
            await promptRepository.SaveAsync(prompt, CancellationToken.None);
            await context.State.WriteAsync();
            throw;
        }
    }

    private async Task<string> ExecuteDirectAsync(
        Prompt prompt,
        ChatHistoryAgentThread executionThread,
        CancellationToken cancellationToken)
    {
        AITaskType intent = prompt.PromptIntent
            ?? throw new InvalidOperationException(
                "Prompt intent analysis produced no result.");
        IPromptStrategy strategy = registeredStrategies.Single(
            strategy => strategy.Intent == intent);

        return await strategy.ExecutionAsync(
            prompt,
            executionThread,
            cancellationToken);
    }

    private static void Validate(
        ExecutionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.ChatId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Content);
        ArgumentNullException.ThrowIfNull(request.ChatHistoryThread);
        cancellationToken.ThrowIfCancellationRequested();
    }
}
