using DefneAI.Domain.Models;

namespace DefneAI.Application.Repository;

public interface IPromptRepository
{
    Task<Prompt> AddAsync(
        Prompt prompt,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Prompt prompt,
        CancellationToken cancellationToken = default);
}
