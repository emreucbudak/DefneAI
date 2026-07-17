using DefneAI.Domain.Models;

namespace DefneAI.Application.Repository;

public interface IChatRepository
{
    Task<IReadOnlyList<Chat>> GetAllWithHistoryAsync(
        CancellationToken cancellationToken = default);

    Task<Chat?> GetByIdWithHistoryAsync(
        int chatId,
        CancellationToken cancellationToken = default);

    Task<Chat> AddAsync(
        Chat chat,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        int chatId,
        CancellationToken cancellationToken = default);
}
