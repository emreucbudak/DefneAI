using DefneAI.Domain.Models;

namespace DefneAI.Application.Repository;

public interface IChatRepository
{
    Task<Chat?> GetLatestWithHistoryAsync(
        CancellationToken cancellationToken = default);

    Task<Chat> AddAsync(
        Chat chat,
        CancellationToken cancellationToken = default);
}
