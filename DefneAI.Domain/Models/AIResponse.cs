namespace DefneAI.Domain.Models;

public sealed class AIResponse
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public int PromptId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public bool IsProposal { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Chat Chat { get; set; } = null!;
    public Prompt Prompt { get; set; } = null!;
}
