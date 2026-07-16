using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.ExecutionService
{
    public interface IModelExecutionService
    {
        Task<string> ExecuteLowSecurityAsync(
            string prompt,
            DefneAI.Domain.Models.PromptAnalysisResult analysis,
            ChatHistoryAgentThread chatHistoryThread,
            CancellationToken cancellationToken = default);

        Task<string> ExecuteElevatedSecurityAsync(
            string prompt,
            DefneAI.Domain.Models.PromptAnalysisResult analysis,
            ChatHistoryAgentThread chatHistoryThread,
            CancellationToken cancellationToken = default);
    }
}
