using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DefneAI.Application.Helpers;
using DefneAI.Application.Repository;
using DefneAI.Domain.Enums;
using DefneAI.Domain.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.PromptAnalysis;

public sealed class PromptAnalysisService(
    ChatCompletionAgent cliBrain,
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
            Analyze the user's request once and return its routing classification.
            Return exactly one JSON object. Do not return markdown or an explanation.

            Allowed intent values:
            - Coding: software development, architecture, debugging, code, model or tool configuration.
            - OfficeTask: documents, spreadsheets, presentations, email or calendar work.
            - WebSearch: information that requires browsing, current data or online research.
            - GeneralChat: conversation, chat-session management, explanation or other requests.

            Use this exact JSON shape:
            {
              "intent": "Coding"
            }

            User request:
            {{prompt.Content}}
            """;

        ChatHistoryAgentThread analysisThread =
            ChatHistoryThreadFactory.CreateCopy(chatHistoryThread);
        StringBuilder responseBuilder = new();

        await foreach (AgentResponseItem<ChatMessageContent> response in
            cliBrain.InvokeAsync(
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
        try
        {
            using JsonDocument document = JsonDocument.Parse(
                modelResponse[jsonStart..(jsonEnd + 1)]);
            JsonElement root = document.RootElement;

            intent = root.GetProperty("intent")
                .Deserialize<AITaskType>(AnalysisJsonOptions);
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

        prompt.ApplyAnalysis(intent);
    }
}
