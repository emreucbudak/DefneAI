using DefneAI.Domain.Models;

namespace DefneAI.Application.PromptFilter;

public sealed class PromptFilterPipeline
{
    private readonly IReadOnlyList<IPromptFilter> filters;

    public PromptFilterPipeline(IEnumerable<IPromptFilter> filters)
    {
        ArgumentNullException.ThrowIfNull(filters);
        this.filters = filters
            .OrderBy(filter => filter.Priority)
            .ToArray();
    }

    public async Task ControlAsync(
        Prompt prompt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        foreach (IPromptFilter filter in filters)
        {
            await filter.ControlAsync(prompt, cancellationToken);
        }
    }
}
