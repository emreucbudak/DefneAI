using DefneAI.Domain.Enums;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.PromptLevelService;

public interface IPromptLevelService
{
    Task<string> ProcessAsync(
        string prompt,
        PromptIntent intent,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default);
}
