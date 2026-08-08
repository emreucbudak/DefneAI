namespace DefneAI.Application.DTOs;

public sealed class PlanDto
{
    public List<string> Steps { get; } = [];
    public string FailureReason { get; set; } = string.Empty;
    public string LastFailedStep { get; set; } = string.Empty;
    public int RetryCount { get; set; }
}
