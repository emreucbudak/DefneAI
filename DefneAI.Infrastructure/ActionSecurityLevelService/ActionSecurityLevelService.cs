using System.Text;
using DefneAI.Application.InitializerService;
using DefneAI.Application.PromptFilter;
using DefneAI.Domain.Enums;
using DefneAI.Domain.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Infrastructure.ActionSecurityLevelService;

public sealed class ActionSecurityLevelService(
    IModelInitializerService modelInitializerService) : IPromptFilter
{
    public int Priority => 3;

    public async Task ControlAsync(
        Prompt prompt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt.Content);
        AITaskType intent = prompt.PromptIntent
            ?? throw new InvalidOperationException("Prompt intent has not been assigned.");
        PromptLevel level = prompt.PromptLevel
            ?? throw new InvalidOperationException("Prompt level has not been assigned.");

        string classificationPrompt = $"""
            Classify only the security risk of the action requested by the user.
            The prompt intent is {intent} and its complexity level is {level}.
            Choose exactly one of these values:
            - LOW: read-only work, generating an answer, or reversible chat-session navigation.
            - MEDIUM: reversible local file changes or non-destructive local execution.
            - HIGH: destructive deletion, persistent model configuration, sensitive database changes, or external side effects.
            - EXTRAHIGH: shell or administrator commands, credentials, secrets, or hard-to-reverse system changes.
            Examples:
            - Showing generated code only in the answer is LOW.
            - Adding generated code to the project is MEDIUM.
            - "/modelekle ...", "/modelsil ...", and "/chatsil ..." are HIGH.
            - Running a PowerShell command is EXTRAHIGH.
            - "/yenichat", "/sohbetler", and "/chatsec ..." are LOW.
            Complexity must not change the security result by itself.
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
        prompt.ActionSecurityLevel = modelResponse.ToUpperInvariant() switch
        {
            "LOW" => ActionSecurityLevel.LOW,
            "MEDIUM" => ActionSecurityLevel.MEDIUM,
            "HIGH" => ActionSecurityLevel.HIGH,
            "EXTRAHIGH" => ActionSecurityLevel.EXTRAHIGH,
            _ => throw new InvalidOperationException(
                $"Action security level model returned an invalid value: '{modelResponse}'.")
        };
    }
}
