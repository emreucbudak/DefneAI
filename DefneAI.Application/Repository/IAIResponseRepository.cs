using DefneAI.Domain.Models;

namespace DefneAI.Application.Repository;

public interface IAIResponseRepository
{
    Task<AIResponse> AddAsync(
        AIResponse response,
        CancellationToken cancellationToken = default);
}
