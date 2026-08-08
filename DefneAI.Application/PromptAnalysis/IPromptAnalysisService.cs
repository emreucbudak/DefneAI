using DefneAI.Domain.Models;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.PromptAnalysis;

public interface IPromptAnalysisService
{
    Task<PromptAnalysisResult> AnalyzeAsync(
        Prompt prompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default);
}
