using Microsoft.SemanticKernel.Agents;
using DefneAI.Domain.Enums;

namespace DefneAI.Application.ExecutionService
{
    public interface IModelExecutionService
    {
        AITaskType TaskType { get; }

        Task<string> ExecuteLowSecurityAsync(
            DefneAI.Domain.Models.Prompt prompt,
            ChatHistoryAgentThread chatHistoryThread,
            CancellationToken cancellationToken = default);

        Task<string> ExecuteElevatedSecurityAsync(
            DefneAI.Domain.Models.Prompt prompt,
            ChatHistoryAgentThread chatHistoryThread,
            CancellationToken cancellationToken = default);
    }
}
