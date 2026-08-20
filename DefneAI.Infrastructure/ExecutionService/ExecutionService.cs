using System.Text;
using DefneAI.Application.Commands;
using DefneAI.Application.Execution;
using DefneAI.Application.InitializerService;
using DefneAI.Application.Repository;
using DefneAI.Domain.Enums;
using DefneAI.Domain.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
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
        IList<HarnessAgent> agents =
            await modelInitializerService.GetHarnessAgentsAsync(intent);
        if (agents.Count == 0)
        {
            throw new InvalidOperationException(
                "Çalıştırılabilir bir AI modeli bulunamadı.");
        }

        List<ChatMessage> messages = chatHistoryThread.ChatHistory
            .Select(message => message.ToChatMessage())
            .ToList();
        messages.Add(new ChatMessage(ChatRole.User, executionPrompt));
        List<Exception> failures = new(agents.Count);

        foreach (HarnessAgent agent in agents)
        {
            StringBuilder responseBuilder = new();

            try
            {
                await foreach (AgentResponseUpdate response in agent.RunStreamingAsync(
                    messages,
                    session: null,
                    cancellationToken: cancellationToken))
                {
                    responseBuilder.Append(response.Text);
                }
            }
            catch (Exception ex) when (
                !cancellationToken.IsCancellationRequested &&
                responseBuilder.Length == 0)
            {
                failures.Add(new InvalidOperationException(
                    $"{agent.Id ?? agent.Name ?? "Unknown"} çalıştırılamadı.",
                    ex));
                continue;
            }

            string result = responseBuilder.ToString().Trim();
            if (string.IsNullOrWhiteSpace(result))
            {
                failures.Add(new InvalidOperationException(
                    $"{agent.Id ?? agent.Name ?? "Unknown"} bir sonuç üretmedi."));
                continue;
            }

            await aiResponseRepository.AddAsync(
                new AIResponse
                {
                    ChatId = prompt.ChatId,
                    PromptId = prompt.Id,
                    Content = result,
                    ModelName = agent.Id ?? agent.Name ?? "Unknown",
                    IsProposal = isProposal
                },
                cancellationToken);

            return result;
        }

        throw new AggregateException(
            "Öncelik sırasındaki AI hesaplarının hiçbiri yanıt üretemedi.",
            failures);
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
