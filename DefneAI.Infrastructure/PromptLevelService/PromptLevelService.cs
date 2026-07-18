using System.Text;
using DefneAI.Application.InitializerService;
using DefneAI.Application.PromptFilter;
using DefneAI.Domain.Enums;
using DefneAI.Domain.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Infrastructure.PromptLevelService;

public sealed class PromptLevelService(
    IModelInitializerService modelInitializerService) : IPromptFilter
{
    public int Priority => 2;

    public async Task ControlAsync(
        Prompt prompt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt.Content);
        AITaskType intent = prompt.PromptIntent
            ?? throw new InvalidOperationException("Prompt intent has not been assigned.");

        string classificationPrompt = $"""
            Classify only the complexity of the user's prompt.
            The prompt intent was classified as {intent}.
            Choose exactly one of these values:
            - LOW: one-step work, simple code generation, reading, listing, or a small clear change.
            - MEDIUM: multiple steps, model configuration, adding or updating a model, or moderate debugging.
            - HIGH: architecture changes, complex debugging, or work affecting several components.
            - EXTRAHIGH: broad autonomous work with many dependent changes or multiple systems.
            Examples:
            - "C# metodu yaz" is LOW.
            - "/modelekle ..." is MEDIUM.
            - Chat session commands such as "/yenichat" and "/chatsil" are LOW.
            Do not classify security or permission requirements in this step.
            Return only the selected value without JSON, quotes, markdown, or explanation.

            User prompt:
            {prompt.Content}
            """;
        StringBuilder responseBuilder = new();
        ChatHistoryAgentThread analysisThread = new();

        await foreach (AgentResponseItem<ChatMessageContent> response in
            modelInitializerService.GetCLIBrain().InvokeAsync(
                classificationPrompt,
                thread: analysisThread,
                cancellationToken: cancellationToken))
        {
            responseBuilder.Append(response.Message.Content);
        }

        string modelResponse = responseBuilder.ToString().Trim();
        prompt.PromptLevel = modelResponse.ToUpperInvariant() switch
        {
            "LOW" => PromptLevel.LOW,
            "MEDIUM" => PromptLevel.MEDIUM,
            "HIGH" => PromptLevel.HIGH,
            "EXTRAHIGH" => PromptLevel.EXTRAHIGH,
            _ => throw new InvalidOperationException(
                $"Prompt level model returned an invalid value: '{modelResponse}'.")
        };
    }
}
