using DefneAI.Application.ExecutionService;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.Router
{
    public sealed class DefneAgentRouter(IModelExecutionService modelExecutionService)
    {
        public ChatHistoryAgentThread ChatHistoryThread { get; } = new();

        public async Task<string> GetPromptResult(string prompt)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
            return await modelExecutionService.GetPromptResult(prompt, ChatHistoryThread);
        }
    }
}
