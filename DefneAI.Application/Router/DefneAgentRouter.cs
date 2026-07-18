using DefneAI.Application.PromptFilter;
using DefneAI.Application.ExecutionService;
using DefneAI.Application.ChatSession;
using DefneAI.Application.Repository;
using DefneAI.Domain.Models;
using DefneAI.Domain.Enums;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.Router
{
    public sealed class DefneAgentRouter(
        PromptFilterPipeline promptFilterPipeline,
        IEnumerable<IModelExecutionService> modelExecutionServices,
        IChatSessionService chatSessionService,
        IPromptRepository promptRepository)
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

            await promptFilterPipeline.ControlAsync(promptRecord, cancellationToken);
            await promptRepository.UpdateAsync(promptRecord, cancellationToken);

            AITaskType taskType = promptRecord.PromptIntent
                ?? throw new InvalidOperationException("Prompt intent has not been assigned.");
            ActionSecurityLevel securityLevel = promptRecord.ActionSecurityLevel
                ?? throw new InvalidOperationException(
                    "Action security level has not been assigned.");
            IModelExecutionService executionService = GetExecutionService(taskType);

            return securityLevel == ActionSecurityLevel.LOW
                ? await executionService.ExecuteLowSecurityAsync(
                    promptRecord, ChatHistoryThread, cancellationToken)
                : await executionService.ExecuteElevatedSecurityAsync(
                    promptRecord, ChatHistoryThread, cancellationToken);
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
