using DefneAI.Application.Commands;
using DefneAI.Application.ChatSession;
using DefneAI.Application.DTOs;
using DefneAI.Application.InitializerService;
using DefneAI.Application.Repository;
using DefneAI.Domain.Models;
using System.Globalization;

namespace DefneAI.Infrastructure.Commands;

public sealed class CommandDispatcher(
    IModelInitializerService modelInitializerService,
    IModelRepository repository,
    IChatSessionService chatSessionService) : ICommandDispatcher
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
                "/yenichat" => await CreateNewChat(arguments, cancellationToken),
                "/sohbetler" => await ListChats(cancellationToken),
                "/chatsec" => await SelectChat(arguments, cancellationToken),
                "/chatsil" => await DeleteChat(arguments, cancellationToken),
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
            "/yenichat - Yeni bir sohbet oluşturur ve ona geçer",
            "/sohbetler - Kayıtlı sohbetleri tarihleriyle listeler",
            "/chatsec {chatId} - Eski bir sohbete geçer",
            "/chatsil [chatId] - Belirtilen veya aktif sohbeti siler",
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

    private async Task<string> CreateNewChat(
        string arguments,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(arguments))
        {
            return "Kullanım: /yenichat";
        }

        Chat chat = await chatSessionService.CreateNewChatAsync(cancellationToken);
        return $"Yeni chat oluşturuldu. Aktif chat: {chat.Id}";
    }

    private async Task<string> ListChats(CancellationToken cancellationToken)
    {
        IReadOnlyList<Chat> chats =
            await chatSessionService.GetChatsAsync(cancellationToken);
        if (chats.Count == 0)
        {
            return "Kayıtlı sohbet bulunamadı.";
        }

        return string.Join(
            Environment.NewLine,
            chats.Select(chat =>
            {
                string activeMarker =
                    chat.Id == chatSessionService.ActiveChatId ? "*" : " ";
                string preview = GetChatPreview(chat);
                string localDate = chat.CreatedAtUtc
                    .ToLocalTime()
                    .ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture);
                string age = GetRelativeAge(chat.CreatedAtUtc);

                return $"{activeMarker} {chat.Id} | {localDate} ({age}) | " +
                    $"{chat.Prompts.Count} prompt, {chat.Responses.Count} cevap | " +
                    preview;
            }));
    }

    private async Task<string> SelectChat(
        string arguments,
        CancellationToken cancellationToken)
    {
        if (!TryParseChatId(arguments, out int chatId))
        {
            return "Kullanım: /chatsec {chatId}";
        }

        bool selected = await chatSessionService.SelectChatAsync(
            chatId,
            cancellationToken);
        return selected
            ? $"Chat {chatId} aktif hale getirildi."
            : $"Chat {chatId} bulunamadı.";
    }

    private async Task<string> DeleteChat(
        string arguments,
        CancellationToken cancellationToken)
    {
        int chatId;
        if (string.IsNullOrWhiteSpace(arguments))
        {
            Chat activeChat =
                await chatSessionService.GetOrCreateActiveChatAsync(cancellationToken);
            chatId = activeChat.Id;
        }
        else if (!TryParseChatId(arguments, out chatId))
        {
            return "Kullanım: /chatsil [chatId]";
        }

        bool deleted = await chatSessionService.DeleteChatAsync(
            chatId,
            cancellationToken);
        return deleted
            ? $"Chat {chatId} silindi. Aktif chat: {chatSessionService.ActiveChatId}"
            : $"Chat {chatId} bulunamadı.";
    }

    private static bool TryParseChatId(string value, out int chatId)
    {
        return int.TryParse(
                value.Trim(),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out chatId) &&
            chatId > 0;
    }

    private static string GetChatPreview(Chat chat)
    {
        string preview = chat.Prompts
            .OrderBy(prompt => prompt.CreatedAtUtc)
            .Select(prompt => prompt.Content.Trim())
            .FirstOrDefault(content =>
                !string.IsNullOrWhiteSpace(content) &&
                !IsChatSessionCommand(content))
            ?? "(boş sohbet)";

        const int maxLength = 60;
        return preview.Length <= maxLength
            ? preview
            : $"{preview[..maxLength]}...";
    }

    private static bool IsChatSessionCommand(string content)
    {
        string command = content
            .Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()?
            .ToLowerInvariant()
            ?? string.Empty;

        return command is "/yenichat" or
            "/sohbetler" or
            "/chatsec" or
            "/chatsil";
    }

    private static string GetRelativeAge(DateTime createdAtUtc)
    {
        TimeSpan age = DateTime.UtcNow - createdAtUtc;
        if (age.TotalDays >= 1)
        {
            return $"{Math.Max(1, (int)age.TotalDays)} gün önce";
        }

        if (age.TotalHours >= 1)
        {
            return $"{Math.Max(1, (int)age.TotalHours)} saat önce";
        }

        return $"{Math.Max(0, (int)age.TotalMinutes)} dakika önce";
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
