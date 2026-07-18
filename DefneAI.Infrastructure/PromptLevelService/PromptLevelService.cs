using DefneAI.Application.ActionSecurityLevelService;
using DefneAI.Application.InitializerService;
using DefneAI.Application.PromptLevelService;
using DefneAI.Domain.Enums;
using DefneAI.Domain.Models;
using DefneAI.Infrastructure.PromptAnalysis;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Infrastructure.PromptLevelService;

public sealed class PromptLevelService(
    IModelInitializerService modelInitializerService,
    IActionSecurityLevelService actionSecurityLevelService) : IPromptLevelService
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
            - Chat session commands such as "/yenichat" and "/chatsil" are LOW.
            Do not classify security or permission requirements in this step.
            """;

        prompt.PromptLevel = await PromptClassificationClient.AnalyzeAsync<PromptLevel>(
            modelInitializerService.GetCLIBrain(),
            prompt.Content,
            criteria,
            "level",
            cancellationToken);

        return await actionSecurityLevelService.ProcessAsync(
            prompt,
            chatHistoryThread,
            cancellationToken);
    }
}
