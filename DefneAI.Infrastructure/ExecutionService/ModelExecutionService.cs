using System.Text;
using DefneAI.Application.Commands;
using DefneAI.Application.ExecutionService;
using DefneAI.Application.InitializerService;
using DefneAI.Domain.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Infrastructure.ExecutionService;

public sealed class ModelExecutionService(
    ICommandDispatcher commandDispatcher,
    IModelInitializerService modelInitializerService) : IModelExecutionService
{
    public async Task<string> ExecuteLowSecurityAsync(
        string prompt,
        PromptAnalysisResult analysis,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(chatHistoryThread);
        cancellationToken.ThrowIfCancellationRequested();

        if (commandDispatcher.IsCommand(prompt))
        {
            return await commandDispatcher.ExecuteAsync(prompt, cancellationToken);
        }

        IList<ChatCompletionAgent> agents =
            await modelInitializerService.GetChatCompletionAgentsAsync();
        ChatCompletionAgent? agent = agents.FirstOrDefault();
        if (agent is null)
        {
            return "Çalıştırılabilir bir AI modeli bulunamadı.";
        }

        StringBuilder responseBuilder = new();
        await foreach (AgentResponseItem<ChatMessageContent> response in agent.InvokeAsync(
            prompt,
            thread: chatHistoryThread,
            cancellationToken: cancellationToken))
        {
            responseBuilder.Append(response.Message.Content);
        }

        string result = responseBuilder.ToString().Trim();
        return string.IsNullOrWhiteSpace(result)
            ? "AI modeli bir sonuç üretmedi."
            : result;
    }

    public async Task<string> ExecuteElevatedSecurityAsync(
        string prompt,
        PromptAnalysisResult analysis,
        ChatHistoryAgentThread chatHistoryThread,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(chatHistoryThread);
        cancellationToken.ThrowIfCancellationRequested();

        string proposalPrompt = $"""
            You are in proposal-only mode.
            Analyze the user's request and propose a concrete solution.
            Do not call tools, execute commands, modify files, or change any state.
            Return only the proposed solution so the user can approve or reject it.

            Classified intent: {analysis.Intent}
            Classified complexity: {analysis.Level}
            Classified action security: {analysis.SecurityLevel}

            Original user request:
            {prompt}
            """;
        string proposedSolution = await ExecuteLowSecurityAsync(
            proposalPrompt,
            analysis,
            chatHistoryThread,
            cancellationToken);

        Console.WriteLine($"Önerilen çözüm:{Environment.NewLine}{proposedSolution}");
        Console.Write("Çözüm uygulansın mı? (y/n): ");
        string? permission = Console.ReadLine()?.Trim();

        if (!string.Equals(permission, "y", StringComparison.OrdinalIgnoreCase))
        {
            return "İşlem kullanıcı tarafından onaylanmadı; önerilen çözüm uygulanmadı.";
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (commandDispatcher.IsCommand(prompt))
        {
            return await commandDispatcher.ExecuteAsync(
                prompt,
                cancellationToken);
        }

        string applicationPrompt = $"""
            The user approved the proposed solution by entering "y".
            Apply the approved solution now. You may use the available tools when necessary.
            Follow the approved solution and do not perform unrelated actions.

            Original user request:
            {prompt}

            Approved solution:
            {proposedSolution}
            """;

        return await ExecuteLowSecurityAsync(
            applicationPrompt,
            analysis,
            chatHistoryThread,
            cancellationToken);
    }
}
