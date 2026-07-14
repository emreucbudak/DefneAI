using DefneAI.Application.ExecutionService;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Infrastructure.ExecutionService
{
    public sealed class ModelExecutionService : IModelExecutionService
    {
        public Task<string> GetPromptResult(string prompt, ChatHistoryAgentThread chatHistoryThread)
        {
            throw new NotImplementedException();
        }
    }
}
