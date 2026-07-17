using DefneAI.Application.Repository;
using DefneAI.Domain.Models;
using DefneAI.Persistence.Db;
using Microsoft.EntityFrameworkCore;

namespace DefneAI.Persistence.Repository;

public sealed class ChatRepository(ModelDbContext context) : IChatRepository
{
    public async Task<IReadOnlyList<Chat>> GetAllWithHistoryAsync(
        CancellationToken cancellationToken = default)
    {
        if (!context.IsDatabaseConfigured)
        {
            return VolatileChatHistoryStore.GetAll();
        }

        return await context.Chats
            .AsNoTracking()
            .Include(chat => chat.Prompts)
            .Include(chat => chat.Responses)
            .AsSplitQuery()
            .OrderByDescending(chat => chat.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<Chat?> GetByIdWithHistoryAsync(
        int chatId,
        CancellationToken cancellationToken = default)
    {
        if (!context.IsDatabaseConfigured)
        {
            return VolatileChatHistoryStore.GetById(chatId);
        }

        return await context.Chats
            .AsNoTracking()
            .Include(chat => chat.Prompts)
            .Include(chat => chat.Responses)
            .AsSplitQuery()
            .FirstOrDefaultAsync(chat => chat.Id == chatId, cancellationToken);
    }

    public async Task<Chat> AddAsync(
        Chat chat,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chat);

        if (!context.IsDatabaseConfigured)
        {
            return VolatileChatHistoryStore.Add(chat);
        }

        await context.Chats.AddAsync(chat, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return chat;
    }

    public async Task<bool> DeleteAsync(
        int chatId,
        CancellationToken cancellationToken = default)
    {
        if (!context.IsDatabaseConfigured)
        {
            return VolatileChatHistoryStore.Delete(chatId);
        }

        Chat? chat = await context.Chats.FindAsync(
            [chatId],
            cancellationToken);
        if (chat is null)
        {
            return false;
        }

        context.Chats.Remove(chat);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
