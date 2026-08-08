using DefneAI.Domain.Models;

namespace DefneAI.Application.Repository;

public interface IPromptRepository
{
    Task<Prompt> AddAsync(
        Prompt prompt,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        Prompt prompt,
        CancellationToken cancellationToken = default);
}
