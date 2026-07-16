using DefneAI.Domain.Enums;

namespace DefneAI.Application.PromptIntentService;

public interface IPromptIntentService
{
    Task<PromptIntent> AnalyzeAsync(
        string prompt,
        CancellationToken cancellationToken = default);
}
