using DefneAI.Domain.Models;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.Planning;

public interface IPlanExecutor
{
    Task<string> ExecuteAsync(
        Prompt prompt,
        ChatHistoryAgentThread executionThread,
        CancellationToken cancellationToken = default);
}
