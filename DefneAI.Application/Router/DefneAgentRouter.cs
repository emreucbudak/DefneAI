using DefneAI.Application.PromptIntentService;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.Router
{
    public sealed class DefneAgentRouter(
        IPromptIntentService promptIntentService)
    {
        public ChatHistoryAgentThread ChatHistoryThread { get; } = new();

        public async Task<string> GetPromptResult(
            string prompt,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

            return await promptIntentService.ProcessAsync(
                prompt,
                ChatHistoryThread,
                cancellationToken);
        }
    }
}
