using System.Text;
using System.Text.Json;
using DefneAI.Application.ExecutionService;
using DefneAI.Application.InitializerService;
using DefneAI.Domain.Enums;
using DefneAI.Domain.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Infrastructure.ExecutionService;

public sealed class ModelExecutionService(
    IModelInitializerService modelInitializerService) : IModelExecutionService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    public async Task<string> GetPromptResult(
        string prompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chatHistoryThread);

        PromptAnalysisResult analysis = await AnalyzePromptAsync(
            prompt,
            chatHistoryThread,
            cancellationToken);
        return $"Promptun intenti: {analysis.Intent}, leveli: {analysis.Level}.";
    }

    public Task<PromptAnalysisResult> AnalyzePromptAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        return AnalyzePromptAsync(prompt, new ChatHistoryAgentThread(), cancellationToken);
    }

    private async Task<PromptAnalysisResult> AnalyzePromptAsync(
        string prompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        ChatCompletionAgent brain = modelInitializerService.GetCLIBrain();
        StringBuilder responseBuilder = new();

        await foreach (AgentResponseItem<ChatMessageContent> response in brain.InvokeAsync(
            prompt,
            thread: chatHistoryThread,
            cancellationToken: cancellationToken))
        {
            responseBuilder.Append(response.Message.Content);
        }

        return ParseAnalysis(responseBuilder.ToString());
    }

    private static PromptAnalysisResult ParseAnalysis(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            throw new InvalidOperationException("CLI brain returned an empty analysis.");
        }

        PromptAnalysisPayload? payload = JsonSerializer.Deserialize<PromptAnalysisPayload>(
            response,
            JsonOptions);

        if (payload is null ||
            !Enum.TryParse(payload.Intent, true, out PromptIntent intent) ||
            !Enum.TryParse(payload.Level, true, out PromptLevel level) ||
            !Enum.IsDefined(intent) ||
            !Enum.IsDefined(level))
        {
            throw new InvalidOperationException($"CLI brain returned an invalid analysis: {response}");
        }

        return new PromptAnalysisResult(intent, level);
    }

    private sealed class PromptAnalysisPayload
    {
        public string? Intent { get; init; }
        public string? Level { get; init; }
    }
}
