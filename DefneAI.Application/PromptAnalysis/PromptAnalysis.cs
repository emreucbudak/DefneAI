using DefneAI.Domain.Enums;

namespace DefneAI.Application.PromptAnalysis;

public sealed record PromptAnalysisResult(
    AITaskType Intent,
    PromptLevel Complexity,
    ActionSecurityLevel SecurityLevel);
