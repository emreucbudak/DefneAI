using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.PromptIntentService;

public interface IPromptIntentService
{
    Task<string> ProcessAsync(
        DefneAI.Domain.Models.Prompt prompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default);
}
