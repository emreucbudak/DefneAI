using DefneAI.Application.ExecutionService;
using Microsoft.SemanticKernel.Agents;
using DefneAI.Application.ActionSecurityLevelService;
using DefneAI.Application.PromptIntentService;
using DefneAI.Application.PromptLevelService;
using DefneAI.Domain.Enums;
using DefneAI.Domain.Models;

namespace DefneAI.Application.Router
{
    public sealed class DefneAgentRouter(
        DefneAI.Application.Commands.ICommandDispatcher commandDispatcher,
        IPromptIntentService promptIntentService,
        IPromptLevelService promptLevelService,
        IActionSecurityLevelService actionSecurityLevelService,
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

            PromptIntent intent = await promptIntentService.AnalyzeAsync(
                prompt,
                cancellationToken);
            PromptLevel level = await promptLevelService.AnalyzeAsync(
                prompt,
                intent,
                cancellationToken);
            ActionSecurityLevel securityLevel =
                await actionSecurityLevelService.AnalyzeAsync(
                    prompt,
                    intent,
                    level,
                    cancellationToken);
            PromptAnalysisResult analysis = new(
                intent,
                level,
                securityLevel);

            return await modelExecutionService.GetPromptResult(
                prompt,
                analysis,
                ChatHistoryThread,
                cancellationToken);
        }
    }
}
