using System.Text.Json;
using DefneAI.Application.Commands;
using DefneAI.Application.InitializerService;
using DefneAI.Application.Repository;
using DefneAI.Domain.Models;

namespace DefneAI.Infrastructure.Commands;

public sealed class CommandDispatcher(
    IModelInitializerService modelInitializerService,
    IModelRepository repository) : ICommandDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    public bool IsCommand(string input)
    {
        return !string.IsNullOrWhiteSpace(input) && input.TrimStart().StartsWith('/');
    }

    public async Task<string> ExecuteAsync(
        string input,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);
        cancellationToken.ThrowIfCancellationRequested();

        (string command, string arguments) = Split(input);
        try
        {
            return command.ToLowerInvariant() switch
            {
                "/komutlar" => GetCommands(),
                "/beyin" => $"Aktif beyin: {modelInitializerService.GetCLIBrain().Description}",
                "/modellistele" => await ListModels(),
                "/modelekle" => await AddModel(arguments),
                "/modelguncelle" => await UpdateModel(arguments),
                "/modelsil" => await RemoveModel(arguments),
                _ => $"Bilinmeyen komut: {command}{Environment.NewLine}/komutlar ile listeyi görüntüle."
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return $"Komut çalıştırılamadı: {ex.Message}";
        }
    }

    private static (string Command, string Arguments) Split(string input)
    {
        string value = input.Trim();
        int separator = value.IndexOf(' ');
        return separator < 0
            ? (value, string.Empty)
            : (value[..separator], value[(separator + 1)..].Trim());
    }

    private static string GetCommands()
    {
        return string.Join(Environment.NewLine, new[]
        {
            "/komutlar - Komut listesini gösterir",
            "/beyin - Aktif yerel beyin modelini gösterir",
            "/modellistele - Kayıtlı modelleri listeler",
            "/modelekle {json} - Yeni model ekler",
            "/modelguncelle {json} - Id alanındaki modeli günceller",
            "/modelsil {modelAdı} - Modeli pasif duruma getirir"
        });
    }

    private async Task<string> ListModels()
    {
        AIModelProvider[] models = (await repository.GetAllModelProviders())
            .OrderBy(model => model.PriorityNumber)
            .ThenBy(model => model.Id)
            .ToArray();

        if (models.Length == 0)
        {
            return "Kayıtlı model bulunamadı.";
        }

        return string.Join(Environment.NewLine, models.Select(model =>
            $"{model.Id} | {model.ModelName} | {model.ModelId} | {model.ServiceId} | " +
            $"Öncelik: {model.PriorityNumber} | Silindi: {model.IsRemoved}"));
    }

    private async Task<string> AddModel(string arguments)
    {
        AIModelProvider? model = DeserializeModel(arguments, out string error);
        if (model is null)
        {
            return error;
        }

        model.Id = 0;
        model.IsRemoved = false;
        return await repository.AddModel(model);
    }

    private async Task<string> UpdateModel(string arguments)
    {
        AIModelProvider? model = DeserializeModel(arguments, out string error);
        if (model is null)
        {
            return error;
        }

        return model.Id <= 0
            ? "Model güncellemek için Id zorunludur."
            : await repository.UpdateModel(model);
    }

    private async Task<string> RemoveModel(string arguments)
    {
        return string.IsNullOrWhiteSpace(arguments)
            ? "Kullanım: /modelsil {modelAdı}"
            : await repository.RemoveModel(arguments.Trim());
    }

    private static AIModelProvider? DeserializeModel(string json, out string error)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            error = GetAddModelUsage();
            return null;
        }

        try
        {
            AIModelProvider? model = JsonSerializer.Deserialize<AIModelProvider>(json, JsonOptions);
            error = model is null ? "Model JSON içeriği boş." : Validate(model);
            return string.IsNullOrEmpty(error) ? model : null;
        }
        catch (JsonException ex)
        {
            error = $"Geçersiz JSON: {ex.Message}{Environment.NewLine}{GetAddModelUsage()}";
            return null;
        }
    }

    private static string Validate(AIModelProvider model)
    {
        string[] required =
        {
            model.ModelId,
            model.ModelName,
            model.ModelSystemPrompt,
            model.ModelDescription,
            model.ModelInstructions,
            model.ApiKey,
            model.Endpoint,
            model.ServiceId
        };

        if (required.Any(string.IsNullOrWhiteSpace))
        {
            return "Modelin metin alanları zorunludur.";
        }

        if (!Uri.TryCreate(model.Endpoint, UriKind.Absolute, out _))
        {
            return "Endpoint geçerli bir mutlak adres olmalıdır.";
        }

        return model.Temperature is < 0 or > 2
            ? "Temperature 0 ile 2 arasında olmalıdır."
            : string.Empty;
    }

    private static string GetAddModelUsage()
    {
        AIModelProvider example = new()
        {
            ModelId = "gpt-4o-mini",
            ModelName = "KodModeli",
            ModelSystemPrompt = "Yazılım asistanı",
            ModelDescription = "Kod görevleri",
            ModelInstructions = "Kısa ve doğru yanıt ver",
            Temperature = 0.2,
            ApiKey = "api-key",
            Endpoint = "https://api.openai.com/v1",
            ServiceId = "coding-model",
            PriorityNumber = 1
        };

        return $"Kullanım: /modelekle {JsonSerializer.Serialize(example, JsonOptions)}";
    }
}
