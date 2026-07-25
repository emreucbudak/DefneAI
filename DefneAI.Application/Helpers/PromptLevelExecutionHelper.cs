using System.Text;
using DefneAI.Application.DTOs;
using DefneAI.Application.Middleware;
using DefneAI.Domain.Enums;
using DefneAI.Domain.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.Helpers;

public static class PromptLevelExecutionHelper
{
    private static readonly RetryMiddleware RetryMiddleware = new();

    public static Task<PromptLevelExecutionResult> LowExecuteAsync(
        IList<ChatCompletionAgent> agents,
        Prompt prompt,
        string executionPrompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAgentAsync(
            agents,
            prompt,
            PromptLevel.LOW,
            executionPrompt,
            chatHistoryThread,
            cancellationToken);
    }

    public static async Task<PromptLevelExecutionResult> MediumExecuteAsync(
        IList<ChatCompletionAgent> agents,
        Prompt prompt,
        string executionPrompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default)
    {
        await LowExecuteAsync(
            agents,
            prompt,
            executionPrompt,
            chatHistoryThread,
            cancellationToken);

        return await ExecuteAgentAsync(
            agents,
            prompt,
            PromptLevel.MEDIUM,
            executionPrompt,
            chatHistoryThread,
            cancellationToken);
    }

    public static async Task<PromptLevelExecutionResult> HighExecuteAsync(
        IList<ChatCompletionAgent> agents,
        Prompt prompt,
        string executionPrompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default)
    {
        await MediumExecuteAsync(
            agents,
            prompt,
            executionPrompt,
            chatHistoryThread,
            cancellationToken);

        return await ExecuteAgentAsync(
            agents,
            prompt,
            PromptLevel.HIGH,
            executionPrompt,
            chatHistoryThread,
            cancellationToken);
    }

    public static async Task<PromptLevelExecutionResult> ExtraHighExecuteAsync(
        IList<ChatCompletionAgent> agents,
        Prompt prompt,
        string executionPrompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default)
    {
        await HighExecuteAsync(
            agents,
            prompt,
            executionPrompt,
            chatHistoryThread,
            cancellationToken);

        return await ExecuteAgentAsync(
            agents,
            prompt,
            PromptLevel.EXTRAHIGH,
            executionPrompt,
            chatHistoryThread,
            cancellationToken);
    }

    private static async Task<PromptLevelExecutionResult> ExecuteAgentAsync(
        IList<ChatCompletionAgent> agents,
        Prompt prompt,
        PromptLevel promptLevel,
        string executionPrompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(agents);
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(executionPrompt);
        ArgumentNullException.ThrowIfNull(chatHistoryThread);
        cancellationToken.ThrowIfCancellationRequested();

        if (agents.Count == 0)
        {
            throw new InvalidOperationException();
        }

        int agentIndex = Math.Min((int)promptLevel, agents.Count - 1);
        ChatCompletionAgent agent = agents[agentIndex];

        ExecutionAttemptResult attemptResult = await RetryMiddleware.ExecuteAsync(
            new ExecutionAttemptContext(
                prompt,
                promptLevel,
                AttemptNumber: 1,
                executionPrompt),
            async (attemptContext, token) => await InvokeAgentAsync(
                agent,
                attemptContext.ExecutionPrompt,
                chatHistoryThread,
                token),
            cancellationToken);

        if (!attemptResult.Success)
        {
            throw new InvalidOperationException(
                $"{promptLevel} execution failed after " +
                $"{attemptResult.AttemptCount} attempts: " +
                attemptResult.FailureReason);
        }

        return new PromptLevelExecutionResult(
            attemptResult.Output ?? string.Empty,
            agent);
    }

    private static async Task<string> InvokeAgentAsync(
        ChatCompletionAgent agent,
        string executionPrompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken)
    {
        StringBuilder responseBuilder = new();

        await foreach (AgentResponseItem<ChatMessageContent> response in agent.InvokeAsync(
            executionPrompt,
            thread: chatHistoryThread,
            cancellationToken: cancellationToken))
        {
            responseBuilder.Append(response.Message.Content);
        }

        return responseBuilder.ToString().Trim();
    }
}

public sealed record PromptLevelExecutionResult(
    string Content,
    ChatCompletionAgent Agent);
