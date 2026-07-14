using System.ComponentModel;
using DefneAI.Application.Repository;
using DefneAI.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace DefneAI.Infrastructure.Plugin
{
    public sealed partial class DefneAutomationPlugin
    {
        [KernelFunction]
        [Description("Kayıtlı AI modellerini API anahtarlarını göstermeden listeler")]
        public async Task<string> ListModels()
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            IModelRepository repository = scope.ServiceProvider.GetRequiredService<IModelRepository>();
            IEnumerable<AIModelProvider> models = await repository.GetAllModelProviders();

            string[] modelSummaries = models
                .OrderBy(model => model.PriorityNumber)
                .Select(model =>
                    $"Id: {model.Id}, Model: {model.ModelName}, ModelId: {model.ModelId}, " +
                    $"ServiceId: {model.ServiceId}, Endpoint: {model.Endpoint}, " +
                    $"Priority: {model.PriorityNumber}, Removed: {model.IsRemoved}")
                .ToArray();

            return modelSummaries.Length == 0
                ? "Kayıtlı model bulunamadı."
                : string.Join(Environment.NewLine, modelSummaries);
        }

        [KernelFunction]
        [Description("Yeni bir AI modeli kaydeder")]
        public async Task<string> AddModel(
            string modelId,
            string modelName,
            string modelSystemPrompt,
            string modelDescription,
            string modelInstructions,
            double temperature,
            string apiKey,
            string endpoint,
            string serviceId,
            int priorityNumber)
        {
            AIModelProvider model = new()
            {
                ModelId = modelId,
                ModelName = modelName,
                ModelSystemPrompt = modelSystemPrompt,
                ModelDescription = modelDescription,
                ModelInstructions = modelInstructions,
                Temperature = temperature,
                ApiKey = apiKey,
                Endpoint = endpoint,
                ServiceId = serviceId,
                PriorityNumber = priorityNumber,
                IsRemoved = false
            };

            using IServiceScope scope = scopeFactory.CreateScope();
            IModelRepository repository = scope.ServiceProvider.GetRequiredService<IModelRepository>();
            return await repository.AddModel(model);
        }

        [KernelFunction]
        [Description("Kayıtlı bir AI modelini tüm alanlarıyla günceller")]
        public async Task<string> UpdateModel(
            int id,
            string modelId,
            string modelName,
            string modelSystemPrompt,
            string modelDescription,
            string modelInstructions,
            double temperature,
            string apiKey,
            string endpoint,
            string serviceId,
            int priorityNumber,
            bool isRemoved = false)
        {
            AIModelProvider model = new()
            {
                Id = id,
                ModelId = modelId,
                ModelName = modelName,
                ModelSystemPrompt = modelSystemPrompt,
                ModelDescription = modelDescription,
                ModelInstructions = modelInstructions,
                Temperature = temperature,
                ApiKey = apiKey,
                Endpoint = endpoint,
                ServiceId = serviceId,
                PriorityNumber = priorityNumber,
                IsRemoved = isRemoved
            };

            using IServiceScope scope = scopeFactory.CreateScope();
            IModelRepository repository = scope.ServiceProvider.GetRequiredService<IModelRepository>();
            return await repository.UpdateModel(model);
        }

        [KernelFunction]
        [Description("Model adına göre kayıtlı AI modelini siler")]
        public async Task<string> RemoveModel(string modelName)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            IModelRepository repository = scope.ServiceProvider.GetRequiredService<IModelRepository>();
            return await repository.RemoveModel(modelName);
        }
    }
}
