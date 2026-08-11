using DefneAI.Application.DTOs;
using DefneAI.Application.ModelFactory;
using DefneAI.Domain.Enums;
using DefneAI.Domain.Models;

namespace DefneAI.Infrastructure.ModelFactory;

public sealed class ModelProviderFactory : IModelProviderFactory
{
    private const string DefaultInstructions =
        "Answer the user's request accurately, clearly, and concisely. " +
        "Use the available tools when they are needed.";

    public AIModelProvider Create(AddModelDto model)
    {
        ArgumentNullException.ThrowIfNull(model);

        string modelName = model.ModelName?.Trim() ?? string.Empty;
        string provider = model.Provider?.Trim() ?? string.Empty;
        string apiKey = model.ApiKey?.Trim() ?? string.Empty;

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

        ProviderSettings settings = GetProviderSettings(provider);

        return new AIModelProvider
        {
            ModelId = modelName,
            ModelName = modelName,
            ModelSystemPrompt = DefaultInstructions,
            ModelDescription = $"{settings.DisplayName} üzerinden çalışan {modelName} modeli.",
            ModelInstructions = DefaultInstructions,
            ModelPurpose = AITaskType.GeneralChat,
            Temperature = 0.7,
            ApiKey = apiKey,
            Endpoint = settings.Endpoint,
            ServiceId = CreateServiceId(settings.Key, modelName),
            PriorityNumber = 100,
            IsRemoved = false
        };
    }

    private static ProviderSettings GetProviderSettings(string provider)
    {
        return Normalize(provider) switch
        {
            "ollama" => new(
                Key: "ollama",
                DisplayName: "Ollama",
                Endpoint: "http://localhost:11434/v1"),
            "lmstudio" => new(
                Key: "lmstudio",
                DisplayName: "LM Studio",
                Endpoint: "http://localhost:1234/v1"),
            "openai" => new(
                Key: "openai",
                DisplayName: "OpenAI",
                Endpoint: "https://api.openai.com/v1"),
            "openrouter" => new(
                Key: "openrouter",
                DisplayName: "OpenRouter",
                Endpoint: "https://openrouter.ai/api/v1"),
            "groq" => new(
                Key: "groq",
                DisplayName: "Groq",
                Endpoint: "https://api.groq.com/openai/v1"),
            "deepseek" => new(
                Key: "deepseek",
                DisplayName: "DeepSeek",
                Endpoint: "https://api.deepseek.com/v1"),
            "gemini" or "google" => new(
                Key: "gemini",
                DisplayName: "Google Gemini",
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

    private static string Normalize(string provider)
    {
        return provider
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
    }

    private sealed record ProviderSettings(
        string Key,
        string DisplayName,
        string Endpoint);
}
