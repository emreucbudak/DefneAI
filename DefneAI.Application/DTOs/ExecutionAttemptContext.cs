using DefneAI.Domain.Enums;
using DefneAI.Domain.Models;

namespace DefneAI.Application.DTOs;

public sealed record ExecutionAttemptContext(
    Prompt Prompt,
    PromptLevel ExecutionLevel,
    int AttemptNumber,
    string ExecutionPrompt,
    string? PreviousOutput = null,
    string? FailureReason = null);
