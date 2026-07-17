using DefneAI.Application.Repository;
using DefneAI.Domain.Models;
using DefneAI.Persistence.Db;
using Microsoft.EntityFrameworkCore;

namespace DefneAI.Persistence.Repository;

public sealed class ChatRepository(ModelDbContext context) : IChatRepository
{
    public async Task<Chat?> GetLatestWithHistoryAsync(
        CancellationToken cancellationToken = default)
    {
        if (context.Database.ProviderName is null)
        {
            return VolatileChatHistoryStore.GetLatest();
        }

        return await context.Chats
            .AsNoTracking()
            .Include(chat => chat.Prompts)
            .Include(chat => chat.Responses)
            .AsSplitQuery()
            .OrderByDescending(chat => chat.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Chat> AddAsync(
        Chat chat,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chat);

        if (context.Database.ProviderName is null)
        {
            return VolatileChatHistoryStore.Add(chat);
        }

        await context.Chats.AddAsync(chat, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return chat;
    }
}
