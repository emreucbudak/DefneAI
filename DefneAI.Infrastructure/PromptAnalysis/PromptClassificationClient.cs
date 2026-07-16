using System.Text;
using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Infrastructure.PromptAnalysis;

internal static class PromptClassificationClient
{
    public static async Task<TEnum> AnalyzeAsync<TEnum>(
        ChatCompletionAgent brain,
        string prompt,
        string criteria,
        string responseProperty,
        CancellationToken cancellationToken)
        where TEnum : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(brain);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(criteria);
        ArgumentException.ThrowIfNullOrWhiteSpace(responseProperty);

        string classificationPrompt = $$"""
            {{criteria}}

            Return only this JSON shape:
            {"{{responseProperty}}":"ENUM_VALUE"}

            User prompt:
            {{prompt}}
            """;
        StringBuilder responseBuilder = new();
        ChatHistoryAgentThread analysisThread = new();

        await foreach (AgentResponseItem<ChatMessageContent> response in brain.InvokeAsync(
            classificationPrompt,
            thread: analysisThread,
            cancellationToken: cancellationToken))
        {
            responseBuilder.Append(response.Message.Content);
        }

        return Parse<TEnum>(responseBuilder.ToString(), responseProperty);
    }

    private static TEnum Parse<TEnum>(string response, string responseProperty)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            throw new InvalidOperationException("CLI brain returned an empty classification.");
        }

        using JsonDocument document = JsonDocument.Parse(response);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                $"CLI brain returned an invalid classification: {response}");
        }

        string? value = null;
        foreach (JsonProperty property in document.RootElement.EnumerateObject())
        {
            if (string.Equals(
                    property.Name,
                    responseProperty,
                    StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value.GetString();
                break;
            }
        }

        if (!Enum.TryParse(value, true, out TEnum result) || !Enum.IsDefined(result))
        {
            throw new InvalidOperationException(
                $"CLI brain returned an invalid classification: {response}");
        }

        return result;
    }
}
