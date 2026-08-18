using DefneAI.Domain.Models;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.Execution;

public interface IExecutionService
{
    Task<string> ExecuteAsync(
        Prompt prompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default);
}
