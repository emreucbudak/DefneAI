using DefneAI.Application.ChatSession;
using DefneAI.Application.Repository;
using DefneAI.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace DefneAI.Infrastructure.ChatSession;

public sealed class ChatSessionService(
    IServiceScopeFactory scopeFactory) : IChatSessionService, IDisposable
{
    private readonly SemaphoreSlim sessionLock = new(1, 1);
    private Chat? activeChat;

    public int? ActiveChatId => activeChat?.Id;
    public ChatHistoryAgentThread ChatHistoryThread { get; private set; } = new();

    public async Task<Chat> GetOrCreateActiveChatAsync(
        CancellationToken cancellationToken = default)
    {
        if (activeChat is not null)
        {
            return activeChat;
        }

        await sessionLock.WaitAsync(cancellationToken);
        try
        {
            if (activeChat is not null)
            {
                return activeChat;
            }

            using IServiceScope scope = scopeFactory.CreateScope();
            IChatRepository repository =
                scope.ServiceProvider.GetRequiredService<IChatRepository>();
            IReadOnlyList<Chat> chats =
                await repository.GetAllWithHistoryAsync(cancellationToken);
            Chat chat = chats.FirstOrDefault()
                ?? await repository.AddAsync(new Chat(), cancellationToken);

            SetActiveChat(chat);
            return chat;
        }
        finally
        {
            sessionLock.Release();
        }
    }

    public async Task<Chat> CreateNewChatAsync(
        CancellationToken cancellationToken = default)
    {
        await sessionLock.WaitAsync(cancellationToken);
        try
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            IChatRepository repository =
                scope.ServiceProvider.GetRequiredService<IChatRepository>();
            Chat chat = await repository.AddAsync(new Chat(), cancellationToken);
            SetActiveChat(chat);
            return chat;
        }
        finally
        {
            sessionLock.Release();
        }
    }

    public async Task<IReadOnlyList<Chat>> GetChatsAsync(
        CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        IChatRepository repository =
            scope.ServiceProvider.GetRequiredService<IChatRepository>();
        return await repository.GetAllWithHistoryAsync(cancellationToken);
    }

    public async Task<bool> SelectChatAsync(
        int chatId,
        CancellationToken cancellationToken = default)
    {
        if (chatId <= 0)
        {
            return false;
        }

        await sessionLock.WaitAsync(cancellationToken);
        try
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            IChatRepository repository =
                scope.ServiceProvider.GetRequiredService<IChatRepository>();
            Chat? chat = await repository.GetByIdWithHistoryAsync(
                chatId,
                cancellationToken);
            if (chat is null)
            {
                return false;
            }

            SetActiveChat(chat);
            return true;
        }
        finally
        {
            sessionLock.Release();
        }
    }

    public async Task<bool> DeleteChatAsync(
        int chatId,
        CancellationToken cancellationToken = default)
    {
        if (chatId <= 0)
        {
            return false;
        }

        await sessionLock.WaitAsync(cancellationToken);
        try
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            IChatRepository repository =
                scope.ServiceProvider.GetRequiredService<IChatRepository>();
            bool deleted = await repository.DeleteAsync(chatId, cancellationToken);
            if (!deleted || activeChat?.Id != chatId)
            {
                return deleted;
            }

            IReadOnlyList<Chat> remainingChats =
                await repository.GetAllWithHistoryAsync(cancellationToken);
            Chat nextChat = remainingChats.FirstOrDefault()
                ?? await repository.AddAsync(new Chat(), cancellationToken);
            SetActiveChat(nextChat);
            return true;
        }
        finally
        {
            sessionLock.Release();
        }
    }

    public void Dispose()
    {
        sessionLock.Dispose();
    }

    private void SetActiveChat(Chat chat)
    {
        ChatHistory history = new();
        IEnumerable<HistoryEntry> entries = chat.Prompts
            .Select(prompt => new HistoryEntry(
                prompt.CreatedAtUtc,
                0,
                AuthorRole.User,
                prompt.Content))
            .Concat(chat.Responses.Select(response => new HistoryEntry(
                response.CreatedAtUtc,
                1,
                AuthorRole.Assistant,
                response.Content)))
            .OrderBy(entry => entry.CreatedAtUtc)
            .ThenBy(entry => entry.RoleOrder);

        foreach (HistoryEntry entry in entries)
        {
            history.AddMessage(entry.Role, entry.Content);
        }

        activeChat = chat;
        ChatHistoryThread = new ChatHistoryAgentThread(
            history,
            $"chat-{chat.Id}");
    }

    private sealed record HistoryEntry(
        DateTime CreatedAtUtc,
        int RoleOrder,
        AuthorRole Role,
        string Content);
}
