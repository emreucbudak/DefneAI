using DefneAI.Application.InitializerService;
using DefneAI.Application.PromptIntentService;
using DefneAI.Application.PromptLevelService;
using DefneAI.Domain.Enums;
using DefneAI.Domain.Models;
using DefneAI.Infrastructure.PromptAnalysis;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Infrastructure.PromptIntentService;

public sealed class PromptIntentService(
    IModelInitializerService modelInitializerService,
    IPromptLevelService promptLevelService) : IPromptIntentService
{
    private const string Criteria = """
        Classify only the user's primary intent.
        Allowed values:
        - Coding: software development, architecture, debugging, code, model or tool configuration.
        - OfficeTask: documents, spreadsheets, presentations, email or calendar work.
        - WebSearch: information that requires browsing, current data or online research.
        - GeneralChat: conversation, chat-session management, explanation or requests outside the other categories.
        Do not classify complexity or security in this step.
        """;

    public async Task<string> ProcessAsync(
        Prompt prompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt.Content);
        ArgumentNullException.ThrowIfNull(chatHistoryThread);

        prompt.PromptIntent = await PromptClassificationClient.AnalyzeAsync<PromptIntent>(
            modelInitializerService.GetCLIBrain(),
            prompt.Content,
            Criteria,
            "intent",
            cancellationToken);

        return await promptLevelService.ProcessAsync(
            prompt,
            chatHistoryThread,
            cancellationToken);
    }
}
