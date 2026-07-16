using DefneAI.Domain.Enums;

namespace DefneAI.Application.ActionSecurityLevelService;

public interface IActionSecurityLevelService
{
    Task<ActionSecurityLevel> AnalyzeAsync(
        string prompt,
        PromptIntent intent,
        PromptLevel level,
        CancellationToken cancellationToken = default);
}
