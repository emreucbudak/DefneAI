using DefneAI.Application.Commands;
using DefneAI.Application.DTOs;
using DefneAI.Application.InitializerService;
using DefneAI.Application.Repository;
using DefneAI.Domain.Models;

namespace DefneAI.Infrastructure.Commands;

public sealed class CommandDispatcher(
    IModelInitializerService modelInitializerService,
    IModelRepository repository) : ICommandDispatcher
{
    public bool IsCommand(string input)
    {
        return !string.IsNullOrWhiteSpace(input) && input.TrimStart().StartsWith('/');
    }

    public async Task<string> AddModelAsync(
        AddModelDto modelDto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(modelDto);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            string validationError = Validate(modelDto);
            if (!string.IsNullOrEmpty(validationError))
            {
                return validationError;
            }

            AIModelProvider model = new()
            {
                ModelId = modelDto.ModelId,
                ModelName = modelDto.ModelName,
                ModelSystemPrompt = modelDto.ModelSystemPrompt,
                ModelDescription = modelDto.ModelDescription,
                ModelInstructions = modelDto.ModelInstructions,
                Temperature = modelDto.Temperature,
                ApiKey = modelDto.ApiKey,
                Endpoint = modelDto.Endpoint,
                ServiceId = modelDto.ServiceId,
                PriorityNumber = modelDto.PriorityNumber,
                IsRemoved = false
            };

            return await repository.AddModel(model);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return $"Model eklenemedi: {ex.Message}";
        }
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
            "/modelguncelle {modelAdı} {argümanAdı} {argümanDeğeri} - Model alanını günceller",
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

    private async Task<string> UpdateModel(string arguments)
    {
        string[] updateArguments = arguments.Split(
            ' ',
            3,
            StringSplitOptions.RemoveEmptyEntries);

        return updateArguments.Length < 3
            ? "Kullanım: /modelguncelle {modelAdı} {argümanAdı} {argümanDeğeri}"
            : await repository.UpdateModel(
                updateArguments[0],
                updateArguments[1],
                updateArguments[2]);
    }

    private async Task<string> RemoveModel(string arguments)
    {
        return string.IsNullOrWhiteSpace(arguments)
            ? "Kullanım: /modelsil {modelAdı}"
            : await repository.RemoveModel(arguments.Trim());
    }

    private static string Validate(AddModelDto model)
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
}
