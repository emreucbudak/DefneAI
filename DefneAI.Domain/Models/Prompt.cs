using DefneAI.Domain.Enums;

namespace DefneAI.Domain.Models;

public sealed class Prompt
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public string Content { get; set; } = string.Empty;
    public PromptState State { get; set; } = PromptState.Thinking;
    public AITaskType? PromptIntent { get; set; }
    public PromptLevel? PromptLevel { get; set; }
    public ActionSecurityLevel? ActionSecurityLevel { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Chat Chat { get; set; } = null!;
    public ICollection<AIResponse> Responses { get; set; } = new List<AIResponse>();
}
