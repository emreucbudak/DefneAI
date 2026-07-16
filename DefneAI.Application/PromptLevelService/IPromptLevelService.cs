using DefneAI.Domain.Enums;

namespace DefneAI.Application.PromptLevelService;

public interface IPromptLevelService
{
    Task<PromptLevel> AnalyzeAsync(
        string prompt,
        PromptIntent intent,
        CancellationToken cancellationToken = default);
}
