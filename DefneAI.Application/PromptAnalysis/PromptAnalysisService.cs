using DefneAI.Application.PromptFilter;
using DefneAI.Domain.Models;

namespace DefneAI.Application.PromptAnalysis;

public sealed class PromptAnalysisService(
    PromptFilterPipeline promptFilterPipeline) : IPromptAnalysisService
{
    public async Task<PromptAnalysisResult> AnalyzeAsync(
        Prompt prompt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        await promptFilterPipeline.ControlAsync(prompt, cancellationToken);

        return new PromptAnalysisResult(
            prompt.PromptIntent
                ?? throw new InvalidOperationException(
                    "Prompt intent analysis produced no result."),
            prompt.PromptLevel
                ?? throw new InvalidOperationException(
                    "Prompt complexity analysis produced no result."),
            prompt.ActionSecurityLevel
                ?? throw new InvalidOperationException(
                    "Prompt security analysis produced no result."));
    }
}
