using DefneAI.Domain.Enums;

namespace DefneAI.Domain.Models
{
    public sealed record PromptAnalysisResult(PromptIntent Intent, PromptLevel Level);
}
