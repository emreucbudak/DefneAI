using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.ExecutionService
{
    public interface IModelExecutionService
    {
        Task<string> GetPromptResult(
            string prompt,
            DefneAI.Domain.Models.PromptAnalysisResult analysis,
            ChatHistoryAgentThread chatHistoryThread,
            CancellationToken cancellationToken = default);
    }
}
