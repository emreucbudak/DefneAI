using DefneAI.Application.ChatSession;
using DefneAI.Application.Repository;
using DefneAI.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace DefneAI.Infrastructure.ChatSession;

public sealed class ChatSessionService(
    IServiceScopeFactory scopeFactory) : IChatSessionService
{
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

        using IServiceScope scope = scopeFactory.CreateScope();
        IChatRepository repository =
            scope.ServiceProvider.GetRequiredService<IChatRepository>();
        IReadOnlyList<Chat> chats =
            await repository.GetAllWithHistoryAsync(cancellationToken);
        Chat chat = chats.FirstOrDefault()
            ?? await repository.AddAsync(new Chat(), cancellationToken);

        SetActiveChatHistory(chat);
        return chat;
    }

    public async Task<Chat> CreateNewChatAsync(
        CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        IChatRepository repository =
            scope.ServiceProvider.GetRequiredService<IChatRepository>();
        Chat chat = await repository.AddAsync(new Chat(), cancellationToken);

        SetActiveChatHistory(chat);
        return chat;
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

        SetActiveChatHistory(chat);
        return true;
    }

    public async Task<bool> DeleteChatAsync(
        int chatId,
        CancellationToken cancellationToken = default)
    {
        if (chatId <= 0)
        {
            return false;
        }

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
        SetActiveChatHistory(nextChat);
        return true;
    }

    private void SetActiveChatHistory(Chat chat)
    {
        ChatHistory history = new();
        IEnumerable<HistoryMessage> messages = chat.Prompts
            .Select(prompt => new HistoryMessage(
                prompt.CreatedAtUtc,
                0,
                IsUser: true,
                prompt.Content))
            .Concat(chat.Responses.Select(response => new HistoryMessage(
                response.CreatedAtUtc,
                1,
                IsUser: false,
                response.Content)))
            .OrderBy(message => message.CreatedAtUtc)
            .ThenBy(message => message.RoleOrder);

        foreach (HistoryMessage message in messages)
        {
            if (message.IsUser)
            {
                history.AddUserMessage(message.Content);
            }
            else
            {
                history.AddAssistantMessage(message.Content);
            }
        }

        activeChat = chat;
        ChatHistoryThread = new ChatHistoryAgentThread(
            history,
            $"chat-{chat.Id}");
    }

    private sealed record HistoryMessage(
        DateTime CreatedAtUtc,
        int RoleOrder,
        bool IsUser,
        string Content);
}
