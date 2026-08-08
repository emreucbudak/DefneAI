using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace DefneAI.Application.Helpers;

public static class ChatHistoryThreadFactory
{
    public static ChatHistoryAgentThread CreateCopy(
        ChatHistoryAgentThread source)
    {
        ArgumentNullException.ThrowIfNull(source);

        ChatHistory history = new(source.ChatHistory);
        return new ChatHistoryAgentThread(history);
    }

    public static void AppendExchange(
        ChatHistoryAgentThread target,
        string userMessage,
        string assistantMessage)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(userMessage);
        ArgumentException.ThrowIfNullOrWhiteSpace(assistantMessage);

        target.ChatHistory.AddUserMessage(userMessage);
        target.ChatHistory.AddAssistantMessage(assistantMessage);
    }
}
