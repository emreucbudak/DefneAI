using DefneAI.Application.Repository;
using DefneAI.Domain.Models;
using DefneAI.Persistence.Db;

namespace DefneAI.Persistence.Repository;

public sealed class AIResponseRepository(ModelDbContext context) : IAIResponseRepository
{
    public async Task<AIResponse> AddAsync(
        AIResponse response,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (context.Database.ProviderName is null)
        {
            return VolatileChatHistoryStore.Add(response);
        }

        await context.AIResponses.AddAsync(response, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return response;
    }
}
