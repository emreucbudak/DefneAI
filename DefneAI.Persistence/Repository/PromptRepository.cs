using DefneAI.Application.Repository;
using DefneAI.Domain.Models;
using DefneAI.Persistence.Db;

namespace DefneAI.Persistence.Repository;

public sealed class PromptRepository(ModelDbContext context) : IPromptRepository
{
    public async Task<Prompt> AddAsync(
        Prompt prompt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        if (!context.IsDatabaseConfigured)
        {
            return VolatileChatHistoryStore.Add(prompt);
        }

        await context.Prompts.AddAsync(prompt, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return prompt;
    }

    public async Task UpdateAsync(
        Prompt prompt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        if (!context.IsDatabaseConfigured)
        {
            VolatileChatHistoryStore.Update(prompt);
            return;
        }

        context.Prompts.Update(prompt);
        await context.SaveChangesAsync(cancellationToken);
    }
}
