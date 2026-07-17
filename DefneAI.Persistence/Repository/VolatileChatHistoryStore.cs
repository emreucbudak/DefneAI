using DefneAI.Domain.Models;

namespace DefneAI.Persistence.Repository;

internal static class VolatileChatHistoryStore
{
    private static readonly object SyncRoot = new();
    private static readonly List<Chat> Chats = [];
    private static int chatId;
    private static int promptId;
    private static int responseId;

    public static IReadOnlyList<Chat> GetAll()
    {
        lock (SyncRoot)
        {
            return Chats
                .OrderByDescending(chat => chat.CreatedAtUtc)
                .ToArray();
        }
    }

    public static Chat? GetById(int id)
    {
        lock (SyncRoot)
        {
            return Chats.FirstOrDefault(chat => chat.Id == id);
        }
    }

    public static Chat Add(Chat chat)
    {
        lock (SyncRoot)
        {
            chat.Id = chat.Id == 0 ? ++chatId : chat.Id;
            chatId = Math.Max(chatId, chat.Id);
            Chats.Add(chat);
            return chat;
        }
    }

    public static Prompt Add(Prompt prompt)
    {
        lock (SyncRoot)
        {
            Chat chat = GetRequiredChat(prompt.ChatId);
            prompt.Id = prompt.Id == 0 ? ++promptId : prompt.Id;
            promptId = Math.Max(promptId, prompt.Id);
            prompt.Chat = chat;
            chat.Prompts.Add(prompt);
            return prompt;
        }
    }

    public static void Update(Prompt prompt)
    {
        lock (SyncRoot)
        {
            Chat chat = GetRequiredChat(prompt.ChatId);
            Prompt? storedPrompt = chat.Prompts.FirstOrDefault(item => item.Id == prompt.Id);
            if (storedPrompt is null)
            {
                throw new InvalidOperationException($"Prompt {prompt.Id} was not found.");
            }

            storedPrompt.Content = prompt.Content;
            storedPrompt.PromptIntent = prompt.PromptIntent;
            storedPrompt.PromptLevel = prompt.PromptLevel;
            storedPrompt.ActionSecurityLevel = prompt.ActionSecurityLevel;
        }
    }

    public static AIResponse Add(AIResponse response)
    {
        lock (SyncRoot)
        {
            Chat chat = GetRequiredChat(response.ChatId);
            Prompt prompt = chat.Prompts.FirstOrDefault(item => item.Id == response.PromptId)
                ?? throw new InvalidOperationException(
                    $"Prompt {response.PromptId} was not found.");

            response.Id = response.Id == 0 ? ++responseId : response.Id;
            responseId = Math.Max(responseId, response.Id);
            response.Chat = chat;
            response.Prompt = prompt;
            chat.Responses.Add(response);
            prompt.Responses.Add(response);
            return response;
        }
    }

    public static bool Delete(int id)
    {
        lock (SyncRoot)
        {
            Chat? chat = Chats.FirstOrDefault(item => item.Id == id);
            if (chat is null)
            {
                return false;
            }

            return Chats.Remove(chat);
        }
    }

    private static Chat GetRequiredChat(int id)
    {
        return Chats.FirstOrDefault(chat => chat.Id == id)
            ?? throw new InvalidOperationException($"Chat {id} was not found.");
    }
}
