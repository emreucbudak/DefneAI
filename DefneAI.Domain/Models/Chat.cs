namespace DefneAI.Domain.Models;

public sealed class Chat
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<Prompt> Prompts { get; set; } = new List<Prompt>();
    public ICollection<AIResponse> Responses { get; set; } = new List<AIResponse>();
}
