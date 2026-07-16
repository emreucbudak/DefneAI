using DefneAI.Application.ExecutionService;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.Router
{
    public sealed class DefneAgentRouter(
        DefneAI.Application.Commands.ICommandDispatcher commandDispatcher,
        IModelExecutionService modelExecutionService)
    {
        public ChatHistoryAgentThread ChatHistoryThread { get; } = new();

        public async Task<string> GetPromptResult(
            string prompt,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

            if (commandDispatcher.IsCommand(prompt))
            {
                return await commandDispatcher.ExecuteAsync(prompt, cancellationToken);
            }

            return await modelExecutionService.GetPromptResult(
                prompt,
                ChatHistoryThread,
                cancellationToken);
        }
    }
}
