using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.ExecutionService
{
    public interface IModelExecutionService
    {
        Task<string> GetPromptResult(string prompt, ChatHistoryAgentThread chatHistoryThread);
    }
}
