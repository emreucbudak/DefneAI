using DefneAI.Domain.Models;

namespace DefneAI.Application.PromptFilter;

public interface IPromptFilter
{
    int Priority { get; }

    Task ControlAsync(
        Prompt prompt,
        CancellationToken cancellationToken = default);
}
