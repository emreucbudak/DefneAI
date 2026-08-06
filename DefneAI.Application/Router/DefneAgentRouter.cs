using DefneAI.Application.ChatSession;
using DefneAI.Application.Planning;
using DefneAI.Application.PromptFilter;
using DefneAI.Application.PromptStates;
using DefneAI.Application.Repository;
using DefneAI.Domain.Enums;
using DefneAI.Domain.Models;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.Router
{
    public sealed class DefneAgentRouter(
        PromptFilterPipeline promptFilterPipeline,
        IChatSessionService chatSessionService,
        IPromptRepository promptRepository,
        IPlanService planService,
        IContext context)
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

            try
            {
                context.State.TransitionTo(context, promptRecord.State);
                await context.State.WriteAsync(
                    () => promptFilterPipeline.ControlAsync(
                        promptRecord,
                        cancellationToken));
                await promptRepository.UpdateAsync(promptRecord, cancellationToken);

                context.State.TransitionTo(context, PromptState.Executing);
                promptRecord.State = PromptState.Executing;
                await promptRepository.UpdateAsync(promptRecord, cancellationToken);

                string? response = null;
                await context.State.WriteAsync(async () =>
                {
                    response = await planService.ExecutePlanAsync(
                        promptRecord,
                        ChatHistoryThread,
                        cancellationToken);
                });

                context.State.TransitionTo(context, PromptState.Completed);
                promptRecord.State = PromptState.Completed;
                await promptRepository.UpdateAsync(promptRecord, cancellationToken);
                await context.State.WriteAsync();

                return response
                    ?? throw new InvalidOperationException(
                        "Prompt strategy returned no response.");
            }
            catch
            {
                context.State.TransitionTo(context, PromptState.Failed);
                promptRecord.State = PromptState.Failed;
                await promptRepository.UpdateAsync(
                    promptRecord,
                    CancellationToken.None);
                await context.State.WriteAsync();
                throw;
            }
        }
    }
}
