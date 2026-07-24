using DefneAI.Application.Commands;
using DefneAI.Application.Helpers;
using DefneAI.Application.InitializerService;
using DefneAI.Application.PromptStrategy;
using DefneAI.Application.Repository;
using DefneAI.Domain.Enums;
using DefneAI.Domain.Models;
using Microsoft.SemanticKernel.Agents;
using Spectre.Console;

namespace DefneAI.Infrastructure.ExecutionService;

public sealed class GeneralChatModelExecutionService(
    ICommandDispatcher commandDispatcher,
    IModelInitializerService modelInitializerService,
    IAIResponseRepository aiResponseRepository) : IPromptStrategy
{
    public AITaskType Intent => AITaskType.GeneralChat;

    public Task<string> ExecutionAsync(
        Prompt prompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default)
    {
        Validate(prompt, chatHistoryThread, cancellationToken);

        return prompt.ActionSecurityLevel switch
        {
            ActionSecurityLevel.LOW => ExecuteLowSecurityAsync(
                prompt,
                chatHistoryThread,
                cancellationToken),
            _ => ExecuteElevatedSecurityAsync(
                prompt,
                chatHistoryThread,
                cancellationToken)
        };
    }

    private async Task<string> ExecuteLowSecurityAsync(
        Prompt prompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken)
    {
        if (commandDispatcher.IsCommand(prompt.Content))
        {
            return await commandDispatcher.ExecuteAsync(
                prompt.Content,
                cancellationToken);
        }

        return await ExecuteModelAsync(
            prompt.Content,
            prompt,
            chatHistoryThread,
            isProposal: false,
            cancellationToken);
    }

    private async Task<string> ExecuteElevatedSecurityAsync(
        Prompt prompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken)
    {
        string proposalPrompt = $"""
            You are in proposal-only mode.
            Analyze the user's request and propose a concrete solution.
            Do not call tools, execute commands, modify files, or change any state.
            Return only the proposed solution so the user can approve or reject it.

            Classified intent: {prompt.PromptIntent}
            Classified complexity: {prompt.PromptLevel}
            Classified action security: {prompt.ActionSecurityLevel}

            Original user request:
            {prompt.Content}
            """;
        string proposedSolution = await ExecuteModelAsync(
            proposalPrompt,
            prompt,
            chatHistoryThread,
            isProposal: true,
            cancellationToken);

        AnsiConsole.MarkupLine("[bold yellow]Önerilen çözüm:[/]");
        AnsiConsole.WriteLine(proposedSolution);
        bool isApproved = AnsiConsole.Confirm(
            "[bold deepskyblue1]Çözüm uygulansın mı?[/]",
            defaultValue: false);

        if (!isApproved)
        {
            return "İşlem kullanıcı tarafından onaylanmadı; önerilen çözüm uygulanmadı.";
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (commandDispatcher.IsCommand(prompt.Content))
        {
            return await commandDispatcher.ExecuteAsync(
                prompt.Content,
                cancellationToken);
        }

        string applicationPrompt = $"""
            The user approved the proposed solution by entering "y".
            Apply the approved solution now. You may use the available tools when necessary.
            Follow the approved solution and do not perform unrelated actions.

            Original user request:
            {prompt.Content}

            Approved solution:
            {proposedSolution}
            """;

        return await ExecuteModelAsync(
            applicationPrompt,
            prompt,
            chatHistoryThread,
            isProposal: false,
            cancellationToken);
    }

    private async Task<string> ExecuteModelAsync(
        string executionPrompt,
        Prompt prompt,
        ChatHistoryAgentThread chatHistoryThread,
        bool isProposal,
        CancellationToken cancellationToken)
    {
        IList<ChatCompletionAgent> agents =
            await modelInitializerService.GetChatCompletionAgentsAsync(Intent);
        if (agents.Count == 0)
        {
            return "Çalıştırılabilir bir AI modeli bulunamadı.";
        }

        PromptLevelExecutionResult executionResult = prompt.PromptLevel switch
        {
            PromptLevel.LOW => await PromptLevelExecutionHelper.LowExecuteAsync(
                agents,
                executionPrompt,
                chatHistoryThread,
                cancellationToken),
            PromptLevel.MEDIUM => await PromptLevelExecutionHelper.MediumExecuteAsync(
                agents,
                executionPrompt,
                chatHistoryThread,
                cancellationToken),
            PromptLevel.HIGH => await PromptLevelExecutionHelper.HighExecuteAsync(
                agents,
                executionPrompt,
                chatHistoryThread,
                cancellationToken),
            PromptLevel.EXTRAHIGH => await PromptLevelExecutionHelper.ExtraHighExecuteAsync(
                agents,
                executionPrompt,
                chatHistoryThread,
                cancellationToken),
            _ => throw new ArgumentOutOfRangeException()
        };

        ChatCompletionAgent agent = executionResult.Agent;
        string result = executionResult.Content;
        if (string.IsNullOrWhiteSpace(result))
        {
            return "AI modeli bir sonuç üretmedi.";
        }

        await aiResponseRepository.AddAsync(
            new AIResponse
            {
                ChatId = prompt.ChatId,
                PromptId = prompt.Id,
                Content = result,
                ModelName = agent.Name ?? agent.Id ?? "Unknown",
                IsProposal = isProposal
            },
            cancellationToken);

        return result;
    }

    private void Validate(
        Prompt prompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt.Content);
        ArgumentNullException.ThrowIfNull(chatHistoryThread);
        cancellationToken.ThrowIfCancellationRequested();

        if (prompt.PromptIntent is null ||
            prompt.PromptLevel is null ||
            prompt.ActionSecurityLevel is null)
        {
            throw new InvalidOperationException(
                "Prompt analysis must be completed before model execution.");
        }

        if (prompt.PromptIntent != Intent)
        {
            throw new InvalidOperationException(
                $"Prompt intent '{prompt.PromptIntent}' does not match strategy intent '{Intent}'.");
        }
    }
}
