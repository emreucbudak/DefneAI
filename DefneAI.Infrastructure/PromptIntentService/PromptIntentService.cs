using DefneAI.Application.InitializerService;
using DefneAI.Application.PromptIntentService;
using DefneAI.Domain.Enums;
using DefneAI.Infrastructure.PromptAnalysis;

namespace DefneAI.Infrastructure.PromptIntentService;

public sealed class PromptIntentService(
    IModelInitializerService modelInitializerService) : IPromptIntentService
{
    private const string Criteria = """
        Classify only the user's primary intent.
        Allowed values:
        - Coding: software development, architecture, debugging, code, model or tool configuration.
        - OfficeTask: documents, spreadsheets, presentations, email or calendar work.
        - WebSearch: information that requires browsing, current data or online research.
        - GeneralChat: conversation, explanation or requests outside the other categories.
        Do not classify complexity or security in this step.
        """;

    public Task<PromptIntent> AnalyzeAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        return PromptClassificationClient.AnalyzeAsync<PromptIntent>(
            modelInitializerService.GetCLIBrain(),
            prompt,
            Criteria,
            "intent",
            cancellationToken);
    }
}
