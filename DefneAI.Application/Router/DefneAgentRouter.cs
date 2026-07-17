using DefneAI.Application.PromptIntentService;
using DefneAI.Application.Repository;
using DefneAI.Domain.Models;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace DefneAI.Application.Router
{
    public sealed class DefneAgentRouter(
        IPromptIntentService promptIntentService,
        IChatRepository chatRepository,
        IPromptRepository promptRepository)
    {
        private Chat? activeChat;

        public ChatHistoryAgentThread ChatHistoryThread { get; private set; } = new();

        public async Task<string> GetPromptResult(
            string prompt,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

            Chat chat = await GetOrCreateChatAsync(cancellationToken);
            Prompt promptRecord = new()
            {
                ChatId = chat.Id,
                Content = prompt
            };
            await promptRepository.AddAsync(promptRecord, cancellationToken);

            return await promptIntentService.ProcessAsync(
                promptRecord,
                ChatHistoryThread,
                cancellationToken);
        }

        private async Task<Chat> GetOrCreateChatAsync(
            CancellationToken cancellationToken)
        {
            if (activeChat is not null)
            {
                return activeChat;
            }

            activeChat = await chatRepository.GetLatestWithHistoryAsync(cancellationToken)
                ?? await chatRepository.AddAsync(new Chat(), cancellationToken);

            ChatHistory chatHistory = new();
            IEnumerable<HistoryEntry> historyEntries = activeChat.Prompts
                .Select(prompt => new HistoryEntry(
                    prompt.CreatedAtUtc,
                    0,
                    AuthorRole.User,
                    prompt.Content))
                .Concat(activeChat.Responses.Select(response => new HistoryEntry(
                    response.CreatedAtUtc,
                    1,
                    AuthorRole.Assistant,
                    response.Content)))
                .OrderBy(entry => entry.CreatedAtUtc)
                .ThenBy(entry => entry.RoleOrder);

            foreach (HistoryEntry entry in historyEntries)
            {
                chatHistory.AddMessage(entry.Role, entry.Content);
            }

            ChatHistoryThread = new ChatHistoryAgentThread(
                chatHistory,
                $"chat-{activeChat.Id}");
            return activeChat;
        }

        private sealed record HistoryEntry(
            DateTime CreatedAtUtc,
            int RoleOrder,
            AuthorRole Role,
            string Content);
    }
}
