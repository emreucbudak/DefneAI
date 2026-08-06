namespace DefneAI.Application.DTOs;

public sealed class PlanDto
{
    public string[] Steps { get; set; } = [];
    public string FailureReason { get; set; } = string.Empty;
    public string LastFailedStep { get; set; } = string.Empty;
    public int RetryCount { get; set; }
}
