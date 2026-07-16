using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.ExecutionService
{
    public interface IModelExecutionService
    {
        Task<string> GetPromptResult(
            string prompt,
            ChatHistoryAgentThread chatHistoryThread,
            CancellationToken cancellationToken = default);

        Task<DefneAI.Domain.Models.PromptAnalysisResult> AnalyzePromptAsync(
            string prompt,
            CancellationToken cancellationToken = default);
    }
}
