using DefneAI.Domain.Models;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.PromptAnalysis;

public interface IPromptAnalysisService
{
    Task AnalyzeAsync(
        Prompt prompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default,
        bool persistChanges = true);
}
