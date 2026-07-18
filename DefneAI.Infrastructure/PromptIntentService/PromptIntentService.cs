using System.Text;
using DefneAI.Application.InitializerService;
using DefneAI.Application.PromptFilter;
using DefneAI.Domain.Enums;
using DefneAI.Domain.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Infrastructure.PromptIntentService;

public sealed class PromptIntentService(
    IModelInitializerService modelInitializerService) : IPromptFilter
{
    public int Priority => 1;

    public async Task ControlAsync(
        Prompt prompt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt.Content);

        string classificationPrompt = $"""
            Classify only the user's primary intent.
            Choose exactly one of these values:
            - Coding: software development, architecture, debugging, code, model or tool configuration.
            - OfficeTask: documents, spreadsheets, presentations, email or calendar work.
            - WebSearch: information that requires browsing, current data or online research.
            - GeneralChat: conversation, chat-session management, explanation or other requests.
            Do not classify complexity or security in this step.
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
        prompt.PromptIntent = modelResponse.ToUpperInvariant() switch
        {
            "CODING" => AITaskType.Coding,
            "OFFICETASK" => AITaskType.OfficeTask,
            "WEBSEARCH" => AITaskType.WebSearch,
            "GENERALCHAT" => AITaskType.GeneralChat,
            _ => throw new InvalidOperationException(
                $"Prompt intent model returned an invalid value: '{modelResponse}'.")
        };
    }
}
