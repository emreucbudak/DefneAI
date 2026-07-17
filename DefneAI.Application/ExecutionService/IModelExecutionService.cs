using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.ExecutionService
{
    public interface IModelExecutionService
    {
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
