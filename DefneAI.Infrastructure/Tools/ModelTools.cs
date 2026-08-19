using System.ComponentModel;
using DefneAI.Application.Commands;
using DefneAI.Application.DTOs;
using Microsoft.Extensions.DependencyInjection;

namespace DefneAI.Infrastructure.Tools;

public sealed class ModelTools(IServiceScopeFactory scopeFactory)
{
    [Description("Kayıtlı AI modellerini API anahtarlarını göstermeden listeler")]
    public Task<string> ListModels()
    {
        return DispatchCommandAsync("/modellistele");
    }

    [Description("Model adı, sağlayıcı, API key, amaç, açıklama, temperature ve priority ile yeni bir AI modeli kaydeder")]
    public Task<string> AddModel(
        string modelName,
        string provider,
        string apiKey,
        string modelPurpose,
        string modelDescription,
        double temperature,
        int priorityNumber)
    {
        AddModelDto model = new(
            ModelName: modelName,
            Provider: provider,
            ApiKey: apiKey,
            ModelPurpose: modelPurpose,
            ModelDescription: modelDescription,
            Temperature: temperature,
            PriorityNumber: priorityNumber);

        return DispatchAddModelAsync(model);
    }

    [Description("Kayıtlı bir AI modelinin belirtilen alanını günceller")]
    public Task<string> UpdateModel(
        string modelName,
        string argumentName,
        string argumentValue)
    {
        return DispatchCommandAsync(
            $"/modelguncelle {modelName} {argumentName} {argumentValue}");
    }

    [Description("Model adına göre kayıtlı AI modelini siler")]
    public Task<string> RemoveModel(string modelName)
    {
        return DispatchCommandAsync($"/modelsil {modelName}");
    }

    private async Task<string> DispatchAddModelAsync(
        AddModelDto model,
        CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        ICommandDispatcher commandDispatcher =
            scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        return await commandDispatcher.AddModelAsync(
            model,
            cancellationToken);
    }

    private async Task<string> DispatchCommandAsync(
        string command,
        CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        ICommandDispatcher commandDispatcher =
            scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        return await commandDispatcher.ExecuteAsync(
            command,
            cancellationToken);
    }
}
