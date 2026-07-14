using DefneAI.Application.KernelFactory;
using DefneAI.Domain.Models;
using DefneAI.Infrastructure.Plugin;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.SemanticKernel;

namespace DefneAI.Infrastructure.KernelFactory
{
    public sealed class DynamicKernelFactory(IMemoryCache cache, DefneAutomationPlugin automationPlugin) : IKernelFactory
    {
        private const string KernelCacheKey = "DefneAI:DynamicKernel";

        public Kernel CreateKernel(IReadOnlyCollection<AIModelProvider> models)
        {
            ArgumentNullException.ThrowIfNull(models);

            IKernelBuilder builder = Kernel.CreateBuilder();
            builder.Plugins.AddFromObject(automationPlugin);

            foreach (AIModelProvider model in models)
            {
                builder.AddOpenAIChatCompletion(
                    modelId: model.ModelId,
                    apiKey: model.ApiKey,
                    endpoint: new Uri(model.Endpoint, UriKind.Absolute),
                    serviceId: model.ServiceId);
            }

            Kernel kernel = builder.Build();
            cache.Set(
                KernelCacheKey,
                kernel,
                new MemoryCacheEntryOptions
                {
                    Priority = CacheItemPriority.NeverRemove
                });

            return kernel;
        }

        public Kernel? GetCachedKernel()
        {
            return cache.Get<Kernel>(KernelCacheKey);
        }

        public void Invalidate()
        {
            cache.Remove(KernelCacheKey);
        }
    }
}
