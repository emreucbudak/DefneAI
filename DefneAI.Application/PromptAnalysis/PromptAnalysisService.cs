using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DefneAI.Application.Helpers;
using DefneAI.Application.InitializerService;
using DefneAI.Application.Repository;
using DefneAI.Domain.Enums;
using DefneAI.Domain.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.PromptAnalysis;

public sealed class PromptAnalysisService(
    IModelInitializerService modelInitializerService,
    IPromptRepository promptRepository) : IPromptAnalysisService
{
    private static readonly JsonSerializerOptions AnalysisJsonOptions = new()
    {
        Converters =
        {
            new JsonStringEnumConverter(allowIntegerValues: false)
        }
    };

    public async Task AnalyzeAsync(
        Prompt prompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default,
        bool persistChanges = true)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(chatHistoryThread);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt.Content);
        cancellationToken.ThrowIfCancellationRequested();

        string analysisPrompt = $$"""
            Analyze the user's request once and return its routing classifications.
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

            Rules:
            - Complexity must not change security by itself.

            Use this exact JSON shape:
            {
              "intent": "Coding",
              "complexity": "LOW",
              "security": "LOW"
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

        ApplyModelResponse(
            prompt,
            responseBuilder.ToString());

        if (persistChanges)
        {
            await promptRepository.SaveAsync(prompt, cancellationToken);
        }
    }

    private static void ApplyModelResponse(
        Prompt prompt,
        string modelResponse)
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

        AITaskType intent;
        PromptLevel complexity;
        ActionSecurityLevel securityLevel;
        try
        {
            using JsonDocument document = JsonDocument.Parse(
                modelResponse[jsonStart..(jsonEnd + 1)]);
            JsonElement root = document.RootElement;

            intent = root.GetProperty("intent")
                .Deserialize<AITaskType>(AnalysisJsonOptions);
            complexity = root.GetProperty("complexity")
                .Deserialize<PromptLevel>(AnalysisJsonOptions);
            securityLevel = root.GetProperty("security")
                .Deserialize<ActionSecurityLevel>(AnalysisJsonOptions);
        }
        catch (Exception exception) when (
            exception is JsonException or
            KeyNotFoundException or
            InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Prompt analysis model returned invalid analysis JSON: '{modelResponse.Trim()}'.",
                exception);
        }

        prompt.ApplyAnalysis(
            intent,
            complexity,
            securityLevel);
    }
}
