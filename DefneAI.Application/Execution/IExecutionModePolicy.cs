using DefneAI.Application.PromptAnalysis;
using DefneAI.Domain.Enums;
using DefneAI.Domain.Models;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.Execution;

public interface IExecutionModePolicy
{
    Task<ExecutionMode> DetermineAsync(
        Prompt prompt,
        PromptAnalysisResult analysis,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default);
}
