using DefneAI.Application.DTOs;
using DefneAI.Application.ModelFactory;
using DefneAI.Domain.Enums;
using DefneAI.Domain.Models;
using FluentValidation;

namespace DefneAI.Infrastructure.ModelFactory;

public sealed class ModelProviderFactory(
    IValidator<AddModelDto> validator) : IModelProviderFactory
{
    public AIModelProvider Create(AddModelDto model)
    {
        validator.ValidateAndThrow(model);

        string modelName = model.ModelName.Trim();
        string provider = model.Provider.Trim();
        string apiKey = model.ApiKey.Trim();
        string modelPurposeName = model.ModelPurpose.Trim();
        string modelDescription = model.ModelDescription.Trim();

        AITaskType modelPurpose = GetModelPurpose(modelPurposeName);
        ProviderSettings settings = GetProviderSettings(provider);

        return new AIModelProvider
        {
            ModelId = modelName,
            ModelName = modelName,
            ModelDescription = modelDescription,
            ModelPurpose = modelPurpose,
            Temperature = model.Temperature,
            ApiKey = apiKey,
            Endpoint = settings.Endpoint,
            ServiceId = CreateServiceId(settings.Key, modelName),
            PriorityNumber = model.PriorityNumber,
            IsRemoved = false
        };
    }

    private static AITaskType GetModelPurpose(string modelPurpose)
    {
        return Normalize(modelPurpose) switch
        {
            "coding" or "code" or "kodlama" => AITaskType.Coding,
            "office" or "officetask" or "ofis" => AITaskType.OfficeTask,
            "web" or "websearch" or "arama" => AITaskType.WebSearch,
            "chat" or "generalchat" or "sohbet" => AITaskType.GeneralChat,
            _ => throw new ArgumentException(
                $"Desteklenmeyen model amacı: {modelPurpose}. " +
                "Desteklenenler: coding, office, websearch, chat.",
                nameof(modelPurpose))
        };
    }

    private static ProviderSettings GetProviderSettings(string provider)
    {
        return Normalize(provider) switch
        {
            "ollama" => new(
                Key: "ollama",
                Endpoint: "http://localhost:11434/v1"),
            "lmstudio" => new(
                Key: "lmstudio",
                Endpoint: "http://localhost:1234/v1"),
            "openai" => new(
                Key: "openai",
                Endpoint: "https://api.openai.com/v1"),
            "openrouter" => new(
                Key: "openrouter",
                Endpoint: "https://openrouter.ai/api/v1"),
            "groq" => new(
                Key: "groq",
                Endpoint: "https://api.groq.com/openai/v1"),
            "deepseek" => new(
                Key: "deepseek",
                Endpoint: "https://api.deepseek.com/v1"),
            "gemini" or "google" => new(
                Key: "gemini",
                Endpoint: "https://generativelanguage.googleapis.com"),
            _ => throw new ArgumentException(
                $"Desteklenmeyen sağlayıcı: {provider}. " +
                "Desteklenenler: ollama, lmstudio, openai, openrouter, groq, deepseek, gemini.",
                nameof(provider))
        };
    }

    private static string CreateServiceId(string provider, string modelName)
    {
        string normalizedModelName = new(
            modelName
                .ToLowerInvariant()
                .Select(character => char.IsLetterOrDigit(character) ? character : '-')
                .ToArray());

        while (normalizedModelName.Contains("--", StringComparison.Ordinal))
        {
            normalizedModelName = normalizedModelName.Replace(
                "--",
                "-",
                StringComparison.Ordinal);
        }

        normalizedModelName = normalizedModelName.Trim('-');
        string accountSuffix = Guid.NewGuid().ToString("N")[..8];
        return string.IsNullOrEmpty(normalizedModelName)
            ? $"{provider}-{accountSuffix}"
            : $"{provider}-{normalizedModelName}-{accountSuffix}";
    }

    private static string Normalize(string value)
    {
        return value
            .Trim()
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
    }

    private sealed record ProviderSettings(
        string Key,
        string Endpoint);
}
