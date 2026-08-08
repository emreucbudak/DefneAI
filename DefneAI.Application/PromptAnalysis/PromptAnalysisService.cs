using System.Text;
using System.Text.Json;
using DefneAI.Application.Helpers;
using DefneAI.Application.InitializerService;
using DefneAI.Domain.Enums;
using DefneAI.Domain.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.PromptAnalysis;

public sealed class PromptAnalysisService(
    IModelInitializerService modelInitializerService) : IPromptAnalysisService
{
    public async Task<PromptAnalysisResult> AnalyzeAsync(
        Prompt prompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(chatHistoryThread);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt.Content);
        cancellationToken.ThrowIfCancellationRequested();

        string analysisPrompt = $$"""
            Analyze the user's request once and return every routing classification.
            Return exactly one JSON object. Do not return markdown or an explanation.

            Allowed intent values:
            - Coding: software development, architecture, debugging, code, model or tool configuration.
            - OfficeTask: documents, spreadsheets, presentations, email or calendar work.
            - WebSearch: information that requires browsing, current data or online research.
            - GeneralChat: conversation, chat-session management, explanation or other requests.

            Allowed complexity values:
            - LOW: one-step work, reading, listing, simple generation, or a small clear change.
            - MEDIUM: multiple steps, model configuration, adding or updating a model, or moderate debugging.
            - HIGH: architecture changes, complex debugging, or work affecting several components.
            - EXTRAHIGH: broad autonomous work with many dependent changes or multiple systems.

            Allowed security values:
            - LOW: read-only work, generating an answer, or reversible chat-session navigation.
            - MEDIUM: reversible local file changes or non-destructive local execution.
            - HIGH: destructive deletion, persistent configuration, sensitive database changes,
              or external side effects.
            - EXTRAHIGH: shell or administrator commands, credentials, secrets,
              or hard-to-reverse system changes.

            Allowed executionMode values:
            - DIRECT: conversation, explanation, a single action, one tool call,
              one command, or work that does not benefit from decomposition.
            - PLANNED: multiple dependent actions, work spanning distinct task types,
              or execution where step-level retry and replanning are useful.

            Rules:
            - Complexity must not change security by itself.
            - Complexity and security alone do not require a plan.
            - A difficult one-step request can be DIRECT.
            - Commands beginning with "/" must be DIRECT.

            Use this exact JSON shape:
            {
              "intent": "Coding",
              "complexity": "LOW",
              "security": "LOW",
              "executionMode": "DIRECT"
            }

            User request:
            {{prompt.Content}}
            """;

        ChatHistoryAgentThread analysisThread =
            ChatHistoryThreadFactory.CreateCopy(chatHistoryThread);
        StringBuilder responseBuilder = new();

        await foreach (AgentResponseItem<ChatMessageContent> response in
            modelInitializerService.GetCLIBrain().InvokeAsync(
                analysisPrompt,
                thread: analysisThread,
                cancellationToken: cancellationToken))
        {
            responseBuilder.Append(response.Message.Content);
        }

        ModelPromptAnalysis modelAnalysis = ParseModelResponse(
            responseBuilder.ToString());

        ExecutionMode executionMode =
            prompt.Content.TrimStart().StartsWith('/')
                ? ExecutionMode.Direct
                : ParseEnum<ExecutionMode>(
                    modelAnalysis.ExecutionMode,
                    "executionMode");

        PromptAnalysisResult result = new(
            ParseEnum<AITaskType>(modelAnalysis.Intent, "intent"),
            ParseEnum<PromptLevel>(modelAnalysis.Complexity, "complexity"),
            ParseEnum<ActionSecurityLevel>(modelAnalysis.Security, "security"),
            executionMode);

        prompt.ApplyAnalysis(
            result.Intent,
            result.Complexity,
            result.SecurityLevel);

        return result;
    }

    private static ModelPromptAnalysis ParseModelResponse(string modelResponse)
    {
        if (string.IsNullOrWhiteSpace(modelResponse))
        {
            throw new InvalidOperationException(
                "Prompt analysis model returned an empty response.");
        }

        int jsonStart = modelResponse.IndexOf('{');
        int jsonEnd = modelResponse.LastIndexOf('}');
        if (jsonStart < 0 || jsonEnd <= jsonStart)
        {
            throw new InvalidOperationException(
                $"Prompt analysis model returned invalid JSON: '{modelResponse.Trim()}'.");
        }

        try
        {
            return JsonSerializer.Deserialize<ModelPromptAnalysis>(
                modelResponse[jsonStart..(jsonEnd + 1)],
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })
                ?? throw new InvalidOperationException(
                    "Prompt analysis model returned an empty JSON object.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Prompt analysis model returned invalid JSON: '{modelResponse.Trim()}'.",
                exception);
        }
    }

    private static TEnum ParseEnum<TEnum>(
        string? value,
        string propertyName)
        where TEnum : struct, Enum
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            string? enumName = Enum.GetNames<TEnum>().FirstOrDefault(
                name => name.Equals(
                    value.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            if (enumName is not null)
            {
                return Enum.Parse<TEnum>(enumName);
            }
        }

        throw new InvalidOperationException(
            $"Prompt analysis returned an invalid {propertyName}: '{value}'.");
    }

    private sealed record ModelPromptAnalysis(
        string? Intent,
        string? Complexity,
        string? Security,
        string? ExecutionMode);
}
