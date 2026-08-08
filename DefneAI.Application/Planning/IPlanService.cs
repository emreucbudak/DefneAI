using DefneAI.Application.DTOs;
using DefneAI.Domain.Models;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.Planning;

public interface IPlanService
{
    Task<PlanDto> CreatePlanAsync(
        Prompt prompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default);

    Task RebuildPlanAsync(
        Prompt prompt,
        PlanDto plan,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default);
}
