using DefneAI.Application.PromptIntentService;
using DefneAI.Application.ChatSession;
using DefneAI.Application.Repository;
using DefneAI.Domain.Models;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.Router
{
    public sealed class DefneAgentRouter(
        IPromptIntentService promptIntentService,
        IChatSessionService chatSessionService,
        IPromptRepository promptRepository)
    {
        public ChatHistoryAgentThread ChatHistoryThread =>
            chatSessionService.ChatHistoryThread;

        public async Task<string> GetPromptResult(
            string prompt,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

            Chat chat = await chatSessionService.GetOrCreateActiveChatAsync(
                cancellationToken);
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
    }
}
