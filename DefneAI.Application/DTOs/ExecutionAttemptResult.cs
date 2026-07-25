namespace DefneAI.Application.DTOs;

public sealed record ExecutionAttemptResult(
    bool Success,
    string? Output,
    string? FailureReason,
    int AttemptCount);
