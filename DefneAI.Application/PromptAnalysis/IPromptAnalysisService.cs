using DefneAI.Domain.Models;

namespace DefneAI.Application.PromptAnalysis;

public interface IPromptAnalysisService
{
    Task<PromptAnalysisResult> AnalyzeAsync(
        Prompt prompt,
        CancellationToken cancellationToken = default);
}
