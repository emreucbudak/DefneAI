using System.Text;
using DefneAI.Domain.Enums;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.Helpers;

public static class PromptLevelExecutionHelper
{
    public static Task<PromptLevelExecutionResult> LowExecuteAsync(
        IList<ChatCompletionAgent> agents,
        string executionPrompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAgentAsync(
            agents,
            PromptLevel.LOW,
            executionPrompt,
            chatHistoryThread,
            cancellationToken);
    }

    public static async Task<PromptLevelExecutionResult> MediumExecuteAsync(
        IList<ChatCompletionAgent> agents,
        string executionPrompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default)
    {
        await LowExecuteAsync(
            agents,
            executionPrompt,
            chatHistoryThread,
            cancellationToken);

        return await ExecuteAgentAsync(
            agents,
            PromptLevel.MEDIUM,
            executionPrompt,
            chatHistoryThread,
            cancellationToken);
    }

    public static async Task<PromptLevelExecutionResult> HighExecuteAsync(
        IList<ChatCompletionAgent> agents,
        string executionPrompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default)
    {
        await MediumExecuteAsync(
            agents,
            executionPrompt,
            chatHistoryThread,
            cancellationToken);

        return await ExecuteAgentAsync(
            agents,
            PromptLevel.HIGH,
            executionPrompt,
            chatHistoryThread,
            cancellationToken);
    }

    public static async Task<PromptLevelExecutionResult> ExtraHighExecuteAsync(
        IList<ChatCompletionAgent> agents,
        string executionPrompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default)
    {
        await HighExecuteAsync(
            agents,
            executionPrompt,
            chatHistoryThread,
            cancellationToken);

        return await ExecuteAgentAsync(
            agents,
            PromptLevel.EXTRAHIGH,
            executionPrompt,
            chatHistoryThread,
            cancellationToken);
    }

    private static async Task<PromptLevelExecutionResult> ExecuteAgentAsync(
        IList<ChatCompletionAgent> agents,
        PromptLevel promptLevel,
        string executionPrompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(agents);
        ArgumentException.ThrowIfNullOrWhiteSpace(executionPrompt);
        ArgumentNullException.ThrowIfNull(chatHistoryThread);
        cancellationToken.ThrowIfCancellationRequested();

        if (agents.Count == 0)
        {
            throw new InvalidOperationException();
        }

        int agentIndex = Math.Min((int)promptLevel, agents.Count - 1);
        ChatCompletionAgent agent = agents[agentIndex];
        StringBuilder responseBuilder = new();

        await foreach (AgentResponseItem<ChatMessageContent> response in agent.InvokeAsync(
            executionPrompt,
            thread: chatHistoryThread,
            cancellationToken: cancellationToken))
        {
            responseBuilder.Append(response.Message.Content);
        }

        return new PromptLevelExecutionResult(
            responseBuilder.ToString().Trim(),
            agent);
    }
}

public sealed record PromptLevelExecutionResult(
    string Content,
    ChatCompletionAgent Agent);
