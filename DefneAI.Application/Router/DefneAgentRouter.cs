using DefneAI.Application.ChatSession;
using DefneAI.Application.Execution;
using DefneAI.Domain.Models;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.Router;

public sealed class DefneAgentRouter(
    IChatSessionService chatSessionService,
    IExecutionOrchestrator executionOrchestrator)
{
    public ChatHistoryAgentThread ChatHistoryThread =>
        chatSessionService.ChatHistoryThread;

    public async Task<string> GetPromptResult(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        Chat chat = await chatSessionService.GetOrCreateActiveChatAsync(
            cancellationToken);

        return await executionOrchestrator.ExecuteAsync(
            new ExecutionRequest(
                chat.Id,
                prompt,
                ChatHistoryThread),
            cancellationToken);
    }
}
