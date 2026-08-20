using DefneAI.Application.ChatClientFactory;
using DefneAI.Domain.Enums;
using DefneAI.Domain.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using OpenAI;
using System.ClientModel;

namespace DefneAI.Infrastructure.ChatClientFactory;

public sealed class DynamicChatClientFactory(IMemoryCache cache) : IChatClientFactory
{
    private const string ChatClientsCacheKey = "DefneAI:DynamicChatClients";

    public IReadOnlyDictionary<string, IChatClient> CreateChatClients(
        IReadOnlyCollection<AIModelProvider> models)
    {
        ArgumentNullException.ThrowIfNull(models);

        Dictionary<string, IChatClient> clients = new(
            models.Count,
            StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (AIModelProvider model in models)
            {
                if (!clients.TryAdd(model.ServiceId, CreateChatClient(model)))
                {
                    throw new InvalidOperationException(
                        $"ServiceId benzersiz olmalıdır: {model.ServiceId}");
                }
            }
        }
        catch
        {
            DisposeClients(clients.Values);
            throw;
        }

        IReadOnlyDictionary<string, IChatClient> readOnlyClients = clients;
        cache.Set(
            ChatClientsCacheKey,
            readOnlyClients,
            new MemoryCacheEntryOptions
            {
                Priority = CacheItemPriority.NeverRemove
            }.RegisterPostEvictionCallback(
                static (_, value, _, _) =>
                {
                    if (value is IReadOnlyDictionary<string, IChatClient> removedClients)
                    {
                        DisposeClients(removedClients.Values);
                    }
                }));

        return readOnlyClients;
    }

    public IReadOnlyDictionary<string, IChatClient>? GetCachedChatClients()
    {
        return cache.Get<IReadOnlyDictionary<string, IChatClient>>(
            ChatClientsCacheKey);
    }

    public void Invalidate()
    {
        cache.Remove(ChatClientsCacheKey);
    }

    private static IChatClient CreateChatClient(AIModelProvider model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model.ModelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(model.ApiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(model.ServiceId);

        return model.ProviderType switch
        {
            AIProviderType.Gemini =>
                new Google.GenAI.Client(apiKey: model.ApiKey)
                    .AsIChatClient(model.ModelId),
            AIProviderType.OpenAICompatible => CreateOpenAICompatibleClient(model),
            _ => throw new NotSupportedException(
                $"Desteklenmeyen AI sağlayıcısı: {model.ProviderType}")
        };
    }

    private static IChatClient CreateOpenAICompatibleClient(
        AIModelProvider model)
    {
        if (!Uri.TryCreate(model.Endpoint, UriKind.Absolute, out Uri? endpoint))
        {
            throw new InvalidOperationException(
                $"{model.ServiceId} için geçerli bir endpoint bulunamadı.");
        }

        OpenAI.Chat.ChatClient client = new(
            model.ModelId,
            new ApiKeyCredential(model.ApiKey),
            new OpenAIClientOptions
            {
                Endpoint = endpoint
            });

        return client.AsIChatClient();
    }

    private static void DisposeClients(IEnumerable<IChatClient> clients)
    {
        foreach (IChatClient client in clients)
        {
            if (client is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
