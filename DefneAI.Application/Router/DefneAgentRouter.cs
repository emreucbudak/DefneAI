using DefneAI.Application.PromptFilter;
using DefneAI.Application.ChatSession;
using DefneAI.Application.PromptStrategy;
using DefneAI.Application.Repository;
using DefneAI.Application.PromptStates;
using DefneAI.Domain.Models;
using DefneAI.Domain.Enums;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.Router
{
    public sealed class DefneAgentRouter(
        PromptFilterPipeline promptFilterPipeline,
        IEnumerable<IPromptStrategy> promptStrategies,
        IChatSessionService chatSessionService,
        IPromptRepository promptRepository,
        IContext context)
    {
        private readonly IReadOnlyList<IPromptStrategy> registeredStrategies =
            promptStrategies.ToArray();

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

                AITaskType promptIntent = promptRecord.PromptIntent
                    ?? throw new InvalidOperationException(
                        "Prompt intent has not been assigned.");
                IPromptStrategy promptStrategy = GetPromptStrategy(promptIntent);

                context.State.TransitionTo(context, PromptState.Executing);
                promptRecord.State = PromptState.Executing;
                await promptRepository.UpdateAsync(promptRecord, cancellationToken);

                string? response = null;
                await context.State.WriteAsync(async () =>
                {
                    response = await promptStrategy.ExecutionAsync(
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

        private IPromptStrategy GetPromptStrategy(AITaskType promptIntent)
        {
            return promptIntent switch
            {
                AITaskType.Coding => GetRequiredStrategy(AITaskType.Coding),
                AITaskType.OfficeTask => GetRequiredStrategy(AITaskType.OfficeTask),
                AITaskType.WebSearch => GetRequiredStrategy(AITaskType.WebSearch),
                AITaskType.GeneralChat => GetRequiredStrategy(AITaskType.GeneralChat),
                _ => throw new InvalidOperationException(
                    $"Unsupported prompt intent: {promptIntent}.")
            };
        }

        private IPromptStrategy GetRequiredStrategy(AITaskType promptIntent)
        {
            return registeredStrategies.SingleOrDefault(
                       strategy => strategy.Intent == promptIntent)
                   ?? throw new InvalidOperationException(
                       $"No prompt strategy is registered for intent '{promptIntent}'.");
        }
    }
}
