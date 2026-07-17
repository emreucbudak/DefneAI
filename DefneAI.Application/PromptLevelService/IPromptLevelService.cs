using DefneAI.Domain.Enums;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.PromptLevelService;

public interface IPromptLevelService
{
    Task<string> ProcessAsync(
        DefneAI.Domain.Models.Prompt prompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default);
}
