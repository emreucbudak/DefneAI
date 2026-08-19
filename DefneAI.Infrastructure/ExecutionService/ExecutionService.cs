using System.Text;
using DefneAI.Application.Commands;
using DefneAI.Application.Execution;
using DefneAI.Application.InitializerService;
using DefneAI.Application.Repository;
using DefneAI.Domain.Enums;
using DefneAI.Domain.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Spectre.Console;

namespace DefneAI.Infrastructure.ExecutionService;

public sealed class ExecutionService(
    ICommandDispatcher commandDispatcher,
    IModelInitializerService modelInitializerService,
    IAIResponseRepository aiResponseRepository) : IExecutionService
{
    public Task<string> ExecuteAsync(
        Prompt prompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default)
    {
        Validate(prompt, chatHistoryThread, cancellationToken);

        return ExecuteWithApprovalAsync(
            prompt,
            chatHistoryThread,
            cancellationToken);
    }

    private async Task<string> ExecuteWithApprovalAsync(
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
        AITaskType intent = prompt.PromptIntent
            ?? throw new InvalidOperationException(
                "Prompt intent analysis produced no result.");
        IList<ChatCompletionAgent> agents =
            await modelInitializerService.GetChatCompletionAgentsAsync(intent);
        if (agents.Count == 0)
        {
            throw new InvalidOperationException(
                "Çalıştırılabilir bir AI modeli bulunamadı.");
        }

        ChatCompletionAgent agent = agents[0];
        StringBuilder responseBuilder = new();

        await foreach (AgentResponseItem<ChatMessageContent> response in agent.InvokeAsync(
            executionPrompt,
            thread: chatHistoryThread,
            cancellationToken: cancellationToken))
        {
            responseBuilder.Append(response.Message.Content);
        }

        string result = responseBuilder.ToString().Trim();
        if (string.IsNullOrWhiteSpace(result))
        {
            throw new InvalidOperationException(
                "AI modeli bir sonuç üretmedi.");
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

    private static void Validate(
        Prompt prompt,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt.Content);
        ArgumentNullException.ThrowIfNull(chatHistoryThread);
        cancellationToken.ThrowIfCancellationRequested();

        if (prompt.PromptIntent is null)
        {
            throw new InvalidOperationException(
                "Prompt analysis must be completed before model execution.");
        }
    }
}
