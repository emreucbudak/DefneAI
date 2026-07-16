using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.PromptIntentService;

public interface IPromptIntentService
{
    Task<string> ProcessAsync(
        string prompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default);
}
