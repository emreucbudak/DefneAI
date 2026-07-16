using DefneAI.Application.ActionSecurityLevelService;
using DefneAI.Application.InitializerService;
using DefneAI.Domain.Enums;
using DefneAI.Infrastructure.PromptAnalysis;

namespace DefneAI.Infrastructure.ActionSecurityLevelService;

public sealed class ActionSecurityLevelService(
    IModelInitializerService modelInitializerService) : IActionSecurityLevelService
{
    public Task<ActionSecurityLevel> AnalyzeAsync(
        string prompt,
        PromptIntent intent,
        PromptLevel level,
        CancellationToken cancellationToken = default)
    {
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

        return PromptClassificationClient.AnalyzeAsync<ActionSecurityLevel>(
            modelInitializerService.GetCLIBrain(),
            prompt,
            criteria,
            "actionSecurityLevel",
            cancellationToken);
    }
}
