using DefneAI.Application.ActionSecurityLevelService;
using DefneAI.Application.ExecutionService;
using DefneAI.Application.InitializerService;
using DefneAI.Domain.Enums;
using DefneAI.Domain.Models;
using DefneAI.Infrastructure.PromptAnalysis;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Infrastructure.ActionSecurityLevelService;

public sealed class ActionSecurityLevelService(
    IModelInitializerService modelInitializerService,
    IModelExecutionService modelExecutionService) : IActionSecurityLevelService
{
    public async Task<string> ProcessAsync(
        string prompt,
        PromptIntent intent,
        PromptLevel level,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chatHistoryThread);

        string criteria = $"""
            Classify only the security risk of the action requested by the user.
            The prompt intent is {intent} and its complexity level is {level}.
            Allowed values:
            - LOW: read-only work or generating an answer without changing state.
            - MEDIUM: reversible local file changes or non-destructive local execution.
            - HIGH: persistent model or database changes, deletion, or external side effects.
            - EXTRAHIGH: shell or administrator commands, credentials, secrets, or hard-to-reverse system changes.
            Examples:
            - "C# metodu yaz ve cevapta göster" is LOW.
            - "C# kodunu projeye ekle" is MEDIUM.
            - "/modelekle ..." and "/modelsil ..." are HIGH.
            - "PowerShell komutu çalıştır" is EXTRAHIGH.
            Complexity must not lower or raise the security result by itself.
            """;

        ActionSecurityLevel securityLevel =
            await PromptClassificationClient.AnalyzeAsync<ActionSecurityLevel>(
                modelInitializerService.GetCLIBrain(),
                prompt,
                criteria,
                "actionSecurityLevel",
                cancellationToken);
        PromptAnalysisResult analysis = new(
            intent,
            level,
            securityLevel);

        return securityLevel == ActionSecurityLevel.LOW
            ? await modelExecutionService.ExecuteLowSecurityAsync(
                prompt,
                analysis,
                chatHistoryThread,
                cancellationToken)
            : await modelExecutionService.ExecuteElevatedSecurityAsync(
                prompt,
                analysis,
                chatHistoryThread,
                cancellationToken);
    }
}
