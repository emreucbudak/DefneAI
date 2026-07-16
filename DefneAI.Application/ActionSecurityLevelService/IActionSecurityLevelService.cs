using DefneAI.Domain.Enums;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.ActionSecurityLevelService;

public interface IActionSecurityLevelService
{
    Task<string> ProcessAsync(
        string prompt,
        PromptIntent intent,
        PromptLevel level,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default);
}
