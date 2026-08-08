using System.Text;
using DefneAI.Application.Execution;
using DefneAI.Application.Helpers;
using DefneAI.Application.InitializerService;
using DefneAI.Application.PromptAnalysis;
using DefneAI.Domain.Enums;
using DefneAI.Domain.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Infrastructure.ExecutionPolicy;

public sealed class ExecutionModePolicy(
    IModelInitializerService modelInitializerService) : IExecutionModePolicy
{
    public async Task<ExecutionMode> DetermineAsync(
        Prompt prompt,
        PromptAnalysisResult analysis,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(chatHistoryThread);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt.Content);
        cancellationToken.ThrowIfCancellationRequested();

        if (prompt.Content.TrimStart().StartsWith('/'))
        {
            return ExecutionMode.Direct;
        }

        string decisionPrompt = $"""
            Decide how the user's request should be executed.
            Choose exactly one value:
            - DIRECT: conversation, explanation, a single action, one tool call,
              one command, or work that does not benefit from decomposition.
            - PLANNED: multiple dependent actions, work spanning distinct task
              types, or execution where step-level retry and replanning are useful.

            Complexity alone does not require a plan. A difficult one-step request
            can be DIRECT. Security level alone does not require a plan either.
            Return only DIRECT or PLANNED without JSON, markdown, or explanation.

            Classified intent: {analysis.Intent}
            Classified complexity: {analysis.Complexity}
            Classified security: {analysis.SecurityLevel}

            User request:
            {prompt.Content}
            """;

        ChatHistoryAgentThread analysisThread =
            ChatHistoryThreadFactory.CreateCopy(chatHistoryThread);
        StringBuilder responseBuilder = new();

        await foreach (AgentResponseItem<ChatMessageContent> response in
            modelInitializerService.GetCLIBrain().InvokeAsync(
                decisionPrompt,
                thread: analysisThread,
                cancellationToken: cancellationToken))
        {
            responseBuilder.Append(response.Message.Content);
        }

        string modelResponse = responseBuilder.ToString().Trim();
        return modelResponse.ToUpperInvariant() switch
        {
            "DIRECT" => ExecutionMode.Direct,
            "PLANNED" => ExecutionMode.Planned,
            _ => throw new InvalidOperationException(
                $"Execution mode model returned an invalid value: '{modelResponse}'.")
        };
    }
}
