using DefneAI.Application.Commands;
using DefneAI.Application.ChatSession;
using DefneAI.Application.DTOs;
using DefneAI.Application.ModelFactory;
using DefneAI.Application.Repository;
using DefneAI.Domain.Models;
using System.Globalization;

namespace DefneAI.Infrastructure.Commands;

public sealed class CommandDispatcher(
    IModelRepository repository,
    IChatSessionService chatSessionService,
    IModelProviderFactory modelProviderFactory) : ICommandDispatcher
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
            return await SaveModelAsync(modelDto);
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
                "/yenichat" => await CreateNewChat(arguments, cancellationToken),
                "/sohbetler" => await ListChats(cancellationToken),
                "/chatsec" => await SelectChat(arguments, cancellationToken),
                "/chatsil" => await DeleteChat(arguments, cancellationToken),
                "/modelekle" => await AddModel(arguments, cancellationToken),
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
            "/modelekle {modelAdı} {sağlayıcı} {apiKey} {amaç} {temperature} {priority} {açıklama} - Model ekler",
            "/komutlar - Komut listesini gösterir",
            "/yenichat - Yeni bir sohbet oluşturur ve ona geçer",
            "/sohbetler - Kayıtlı sohbetleri tarihleriyle listeler",
            "/chatsec {chatId} - Eski bir sohbete geçer",
            "/chatsil [chatId] - Belirtilen veya aktif sohbeti siler",
            "/modellistele - Kayıtlı modelleri listeler",
            "/modelguncelle {modelAdı/serviceId} {argümanAdı} {argümanDeğeri} - Model alanını günceller",
            "/modelsil {modelAdı/serviceId} - Modeli pasif duruma getirir"
        });
    }

    private async Task<string> AddModel(
        string arguments,
        CancellationToken cancellationToken)
    {
        string[] addArguments = arguments.Split(
            ' ',
            7,
            StringSplitOptions.RemoveEmptyEntries);

        if (addArguments.Length != 7)
        {
            return "Kullanım: /modelekle {modelAdı} {sağlayıcı} {apiKey} " +
                "{amaç} {temperature} {priority} {açıklama}";
        }

        if (!double.TryParse(
                addArguments[4],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double temperature))
        {
            return "Temperature geçerli bir sayı olmalıdır.";
        }

        if (!int.TryParse(
                addArguments[5],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int priorityNumber))
        {
            return "Priority geçerli bir tam sayı olmalıdır.";
        }

        return await AddModelAsync(
            new AddModelDto(
                ModelName: addArguments[0],
                Provider: addArguments[1],
                ApiKey: addArguments[2],
                ModelPurpose: addArguments[3],
                ModelDescription: addArguments[6],
                Temperature: temperature,
                PriorityNumber: priorityNumber),
            cancellationToken);
    }

    private async Task<string> SaveModelAsync(AddModelDto modelDto)
    {
        AIModelProvider model = modelProviderFactory.Create(modelDto);
        return await repository.AddModel(model);
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

}
