using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.Execution;

public sealed record ExecutionRequest(
    int ChatId,
    string Content,
    ChatHistoryAgentThread ChatHistoryThread);
