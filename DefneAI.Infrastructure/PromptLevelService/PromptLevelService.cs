using DefneAI.Application.InitializerService;
using DefneAI.Application.PromptLevelService;
using DefneAI.Domain.Enums;
using DefneAI.Infrastructure.PromptAnalysis;

namespace DefneAI.Infrastructure.PromptLevelService;

public sealed class PromptLevelService(
    IModelInitializerService modelInitializerService) : IPromptLevelService
{
    public Task<PromptLevel> AnalyzeAsync(
        string prompt,
        PromptIntent intent,
        CancellationToken cancellationToken = default)
    {
        string criteria = $"""
            Classify only the complexity of the user's prompt.
            The prompt intent was classified as {intent}.
            Allowed values:
            - LOW: one-step work, simple code generation, reading, listing or a small clear change.
            - MEDIUM: multiple steps, model configuration, adding or updating a model, or moderate debugging.
            - HIGH: architecture changes, complex debugging or work affecting several components.
            - EXTRAHIGH: broad autonomous work with many dependent changes or multiple systems.
            Examples:
            - "C# metodu yaz" is LOW.
            - "/modelekle ..." is MEDIUM.
            Do not classify security or permission requirements in this step.
            """;

        return PromptClassificationClient.AnalyzeAsync<PromptLevel>(
            modelInitializerService.GetCLIBrain(),
            prompt,
            criteria,
            "level",
            cancellationToken);
    }
}
