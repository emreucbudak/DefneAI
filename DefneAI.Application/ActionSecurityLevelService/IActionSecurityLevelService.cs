using DefneAI.Domain.Enums;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.ActionSecurityLevelService;

public interface IActionSecurityLevelService
{
    Task<string> ProcessAsync(
        DefneAI.Domain.Models.Prompt prompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default);
}
