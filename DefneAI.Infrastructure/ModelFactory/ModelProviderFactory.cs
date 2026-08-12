using DefneAI.Application.DTOs;
using DefneAI.Application.ModelFactory;
using DefneAI.Domain.Enums;
using DefneAI.Domain.Models;

namespace DefneAI.Infrastructure.ModelFactory;

public sealed class ModelProviderFactory : IModelProviderFactory
{
    public AIModelProvider Create(AddModelDto model)
    {
        ArgumentNullException.ThrowIfNull(model);

        string modelName = model.ModelName?.Trim() ?? string.Empty;
        string provider = model.Provider?.Trim() ?? string.Empty;
        string apiKey = model.ApiKey?.Trim() ?? string.Empty;
        string modelPurposeName = model.ModelPurpose?.Trim() ?? string.Empty;
        string modelDescription = model.ModelDescription?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(modelName))
        {
            throw new ArgumentException("Model adı boş olamaz.", nameof(model));
        }

        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new ArgumentException("Sağlayıcı adı boş olamaz.", nameof(model));
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API key boş olamaz.", nameof(model));
        }

        if (string.IsNullOrWhiteSpace(modelPurposeName))
        {
            throw new ArgumentException("Model amacı boş olamaz.", nameof(model));
        }

        if (string.IsNullOrWhiteSpace(modelDescription))
        {
            throw new ArgumentException("Model açıklaması boş olamaz.", nameof(model));
        }

        AITaskType modelPurpose = GetModelPurpose(modelPurposeName);

        if (model.Temperature is < 0 or > 2)
        {
            throw new ArgumentOutOfRangeException(
                nameof(model),
                "Temperature 0 ile 2 arasında olmalıdır.");
        }

        if (model.PriorityNumber < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(model),
                "Priority negatif olamaz.");
        }

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
                Endpoint: "https://generativelanguage.googleapis.com/v1beta/openai/"),
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
        return string.IsNullOrEmpty(normalizedModelName)
            ? provider
            : $"{provider}-{normalizedModelName}";
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
