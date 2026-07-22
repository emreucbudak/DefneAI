using DefneAI.Application.PromptFilter;
using DefneAI.Application.ExecutionService;
using DefneAI.Application.ChatSession;
using DefneAI.Application.Repository;
using DefneAI.Application.PromptStatus;
using DefneAI.Domain.Models;
using DefneAI.Domain.Enums;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.Router
{
    public sealed class DefneAgentRouter(
        PromptFilterPipeline promptFilterPipeline,
        IEnumerable<IModelExecutionService> modelExecutionServices,
        IChatSessionService chatSessionService,
        IPromptRepository promptRepository,
        IPromptStatusPresenter promptStatusPresenter)
    {
        private readonly IReadOnlyDictionary<AITaskType, IModelExecutionService> executionServices =
            modelExecutionServices.ToDictionary(service => service.TaskType);

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
                await promptStatusPresenter.ShowWhileAsync(
                    PromptState.Thinking,
                    () => promptFilterPipeline.ControlAsync(
                        promptRecord,
                        cancellationToken));
                await promptRepository.UpdateAsync(promptRecord, cancellationToken);

                AITaskType taskType = promptRecord.PromptIntent
                    ?? throw new InvalidOperationException(
                        "Prompt intent has not been assigned.");
                ActionSecurityLevel securityLevel = promptRecord.ActionSecurityLevel
                    ?? throw new InvalidOperationException(
                        "Action security level has not been assigned.");
                IModelExecutionService executionService = GetExecutionService(taskType);

                promptRecord.State = PromptState.Executing;
                await promptRepository.UpdateAsync(promptRecord, cancellationToken);

                string response = await promptStatusPresenter.ShowWhileAsync(
                    PromptState.Executing,
                    () => securityLevel == ActionSecurityLevel.LOW
                        ? executionService.ExecuteLowSecurityAsync(
                            promptRecord,
                            ChatHistoryThread,
                            cancellationToken)
                        : executionService.ExecuteElevatedSecurityAsync(
                            promptRecord,
                            ChatHistoryThread,
                            cancellationToken));

                promptRecord.State = PromptState.Completed;
                await promptRepository.UpdateAsync(promptRecord, cancellationToken);
                promptStatusPresenter.Show(PromptState.Completed);

                return response;
            }
            catch
            {
                promptRecord.State = PromptState.Failed;
                await promptRepository.UpdateAsync(
                    promptRecord,
                    CancellationToken.None);
                promptStatusPresenter.Show(PromptState.Failed);
                throw;
            }
        }

        private IModelExecutionService GetExecutionService(AITaskType taskType)
        {
            return taskType switch
            {
                AITaskType.Coding => executionServices[AITaskType.Coding],
                AITaskType.OfficeTask => executionServices[AITaskType.OfficeTask],
                AITaskType.WebSearch => executionServices[AITaskType.WebSearch],
                AITaskType.GeneralChat => executionServices[AITaskType.GeneralChat],
                _ => throw new InvalidOperationException(
                    $"Unsupported AI task type: {taskType}.")
            };
        }
    }
}
