using DefneAI.Domain.Enums;

namespace DefneAI.Domain.Models;

public sealed class Prompt
{
    private Prompt()
    {
    }

    private Prompt(
        int id,
        int chatId,
        string content,
        DateTime createdAtUtc)
    {
        Id = id;
        ChatId = chatId;
        Content = content;
        CreatedAtUtc = createdAtUtc;
    }

    public int Id { get; internal set; }
    public int ChatId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public PromptState State { get; private set; } = PromptState.Thinking;
    public AITaskType? PromptIntent { get; private set; }
    public PromptLevel? PromptLevel { get; private set; }
    public ActionSecurityLevel? ActionSecurityLevel { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public Chat Chat { get; internal set; } = null!;
    public ICollection<AIResponse> Responses { get; private set; } =
        new List<AIResponse>();

    public bool IsAnalyzed =>
        PromptIntent is not null &&
        PromptLevel is not null &&
        ActionSecurityLevel is not null;

    public static Prompt Create(int chatId, string content)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chatId);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        return new Prompt(
            id: 0,
            chatId,
            content,
            DateTime.UtcNow);
    }

    public Prompt CreateExecutionStep(string content)
    {
        if (Id <= 0)
        {
            throw new InvalidOperationException(
                "A persisted prompt is required to create an execution step.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        return new Prompt(
            Id,
            ChatId,
            content,
            CreatedAtUtc);
    }

    public void ClassifyIntent(AITaskType intent)
    {
        EnsureThinking();
        EnsureNotAssigned(PromptIntent, nameof(PromptIntent));
        EnsureDefined(intent);

        PromptIntent = intent;
    }

    public void ClassifyComplexity(PromptLevel level)
    {
        EnsureThinking();
        EnsureAssigned(PromptIntent, nameof(PromptIntent));
        EnsureNotAssigned(PromptLevel, nameof(PromptLevel));
        EnsureDefined(level);

        PromptLevel = level;
    }

    public void ClassifyActionSecurity(ActionSecurityLevel level)
    {
        EnsureThinking();
        EnsureAssigned(PromptIntent, nameof(PromptIntent));
        EnsureAssigned(PromptLevel, nameof(PromptLevel));
        EnsureNotAssigned(ActionSecurityLevel, nameof(ActionSecurityLevel));
        EnsureDefined(level);

        ActionSecurityLevel = level;
    }

    public void StartExecution()
    {
        EnsureThinking();
        if (!IsAnalyzed)
        {
            throw new InvalidOperationException(
                "Prompt analysis must be completed before execution starts.");
        }

        State = PromptState.Executing;
    }

    public void Complete()
    {
        if (State != PromptState.Executing)
        {
            throw new InvalidOperationException(
                $"A prompt in state '{State}' cannot be completed.");
        }

        State = PromptState.Completed;
    }

    public void Fail()
    {
        if (State == PromptState.Failed)
        {
            return;
        }

        State = PromptState.Failed;
    }

    private void EnsureThinking()
    {
        if (State != PromptState.Thinking)
        {
            throw new InvalidOperationException(
                $"A prompt in state '{State}' cannot be classified.");
        }
    }

    private static void EnsureAssigned<T>(T? value, string propertyName)
        where T : struct
    {
        if (value is null)
        {
            throw new InvalidOperationException(
                $"{propertyName} must be assigned first.");
        }
    }

    private static void EnsureNotAssigned<T>(T? value, string propertyName)
        where T : struct
    {
        if (value is not null)
        {
            throw new InvalidOperationException(
                $"{propertyName} has already been assigned.");
        }
    }

    private static void EnsureDefined<T>(T value)
        where T : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                $"Unsupported {typeof(T).Name} value.");
        }
    }
}
