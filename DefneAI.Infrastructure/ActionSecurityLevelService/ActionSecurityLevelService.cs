using DefneAI.Application.ActionSecurityLevelService;
using DefneAI.Application.ExecutionService;
using DefneAI.Application.InitializerService;
using DefneAI.Application.Repository;
using DefneAI.Domain.Enums;
using DefneAI.Domain.Models;
using DefneAI.Infrastructure.PromptAnalysis;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Infrastructure.ActionSecurityLevelService;

public sealed class ActionSecurityLevelService(
    IModelInitializerService modelInitializerService,
    IModelExecutionService modelExecutionService,
    IPromptRepository promptRepository) : IActionSecurityLevelService
{
    public async Task<string> ProcessAsync(
        Prompt prompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt.Content);
        ArgumentNullException.ThrowIfNull(chatHistoryThread);
        AITaskType intent = prompt.PromptIntent
            ?? throw new InvalidOperationException("Prompt intent has not been assigned.");
        PromptLevel level = prompt.PromptLevel
            ?? throw new InvalidOperationException("Prompt level has not been assigned.");

        string criteria = $"""
            Classify only the security risk of the action requested by the user.
            The prompt intent is {intent} and its complexity level is {level}.
            Allowed values:
            - LOW: read-only work, generating an answer, or reversible chat-session navigation.
            - MEDIUM: reversible local file changes or non-destructive local execution.
            - HIGH: destructive deletion, persistent model configuration, sensitive database changes, or external side effects.
            - EXTRAHIGH: shell or administrator commands, credentials, secrets, or hard-to-reverse system changes.
            Examples:
            - "C# metodu yaz ve cevapta göster" is LOW.
            - "C# kodunu projeye ekle" is MEDIUM.
            - "/modelekle ..." and "/modelsil ..." are HIGH.
            - "PowerShell komutu çalıştır" is EXTRAHIGH.
            - "/yenichat", "/sohbetler", and "/chatsec ..." are LOW.
            - "/chatsil ..." is HIGH.
            Complexity must not lower or raise the security result by itself.
            """;

        prompt.ActionSecurityLevel =
            await PromptClassificationClient.AnalyzeAsync<ActionSecurityLevel>(
                modelInitializerService.GetCLIBrain(),
                prompt.Content,
                criteria,
                "actionSecurityLevel",
                cancellationToken);
        await promptRepository.UpdateAsync(prompt, cancellationToken);

        return prompt.ActionSecurityLevel == ActionSecurityLevel.LOW
            ? await modelExecutionService.ExecuteLowSecurityAsync(
                prompt,
                chatHistoryThread,
                cancellationToken)
            : await modelExecutionService.ExecuteElevatedSecurityAsync(
                prompt,
                chatHistoryThread,
                cancellationToken);
    }
}
