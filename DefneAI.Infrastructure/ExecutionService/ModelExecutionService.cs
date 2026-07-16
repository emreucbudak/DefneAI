using DefneAI.Application.ExecutionService;
using DefneAI.Domain.Models;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Infrastructure.ExecutionService;

public sealed class ModelExecutionService : IModelExecutionService
{
    public Task<string> GetPromptResult(
        string prompt,
        PromptAnalysisResult analysis,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(chatHistoryThread);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(
            $"Promptun intenti: {analysis.Intent}, leveli: {analysis.Level}, " +
            $"action security leveli: {analysis.SecurityLevel}.");
    }
}
