using DefneAI.Domain.Models;
using Microsoft.Extensions.AI;

namespace DefneAI.Application.ChatClientFactory;

public interface IChatClientFactory
{
    IReadOnlyDictionary<string, IChatClient> CreateChatClients(
        IReadOnlyCollection<AIModelProvider> models);

    IReadOnlyDictionary<string, IChatClient>? GetCachedChatClients();

    void Invalidate();
}
