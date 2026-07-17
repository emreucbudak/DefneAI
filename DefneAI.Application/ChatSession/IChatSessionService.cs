using DefneAI.Domain.Models;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.ChatSession;

public interface IChatSessionService
{
    int? ActiveChatId { get; }
    ChatHistoryAgentThread ChatHistoryThread { get; }

    Task<Chat> GetOrCreateActiveChatAsync(
        CancellationToken cancellationToken = default);

    Task<Chat> CreateNewChatAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Chat>> GetChatsAsync(
        CancellationToken cancellationToken = default);

    Task<bool> SelectChatAsync(
        int chatId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteChatAsync(
        int chatId,
        CancellationToken cancellationToken = default);
}
