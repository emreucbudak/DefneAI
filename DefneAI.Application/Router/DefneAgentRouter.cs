using DefneAI.Application.ChatSession;
using DefneAI.Application.Execution;
using DefneAI.Application.Helpers;
using DefneAI.Application.PromptAnalysis;
using DefneAI.Application.PromptStates;
using DefneAI.Application.Repository;
using DefneAI.Domain.Enums;
using DefneAI.Domain.Models;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.Router;

public sealed class DefneAgentRouter(
    IChatSessionService chatSessionService,
    IPromptAnalysisService promptAnalysisService,
    IExecutionService executionService,
    IPromptRepository promptRepository,
    IContext context)
{
    public ChatHistoryAgentThread ChatHistoryThread =>
        chatSessionService.ChatHistoryThread;

    public async Task<string> GetPromptResult(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        cancellationToken.ThrowIfCancellationRequested();

        Chat chat = await chatSessionService.GetOrCreateActiveChatAsync(
            cancellationToken);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chat.Id);

        Prompt executionPrompt = Prompt.Create(chat.Id, prompt);
        await promptRepository.AddAsync(executionPrompt, cancellationToken);

        try
        {
            context.State.TransitionTo(context, PromptState.Thinking);
            await context.State.WriteAsync(async () =>
            {
                await promptAnalysisService.AnalyzeAsync(
                    executionPrompt,
                    ChatHistoryThread,
                    cancellationToken);
            });

            context.State.TransitionTo(context, PromptState.Executing);
            executionPrompt.StartExecution();

            ChatHistoryAgentThread executionThread =
                ChatHistoryThreadFactory.CreateCopy(ChatHistoryThread);
            string? response = null;
            await context.State.WriteAsync(async () =>
            {
                response = await executionService.ExecuteAsync(
                    executionPrompt,
                    executionThread,
                    cancellationToken);
            });

            if (string.IsNullOrWhiteSpace(response))
            {
                throw new InvalidOperationException(
                    "Prompt execution returned no response.");
            }

            ChatHistoryThreadFactory.AppendExchange(
                ChatHistoryThread,
                prompt,
                response);

            context.State.TransitionTo(context, PromptState.Completed);
            executionPrompt.Complete();
            await promptRepository.SaveAsync(executionPrompt, cancellationToken);
            await context.State.WriteAsync();

            return response;
        }
        catch
        {
            context.State.TransitionTo(context, PromptState.Failed);
            executionPrompt.Fail();
            await promptRepository.SaveAsync(
                executionPrompt,
                CancellationToken.None);
            await context.State.WriteAsync();
            throw;
        }
    }
}
