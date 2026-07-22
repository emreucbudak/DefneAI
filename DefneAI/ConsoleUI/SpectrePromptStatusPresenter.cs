using DefneAI.Application.PromptStatus;
using DefneAI.Domain.Enums;
using Spectre.Console;

namespace DefneAI.ConsoleUI;

public sealed class SpectrePromptStatusPresenter : IPromptStatusPresenter
{
    public Task ShowWhileAsync(
        PromptState state,
        Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return CreateStatus(state).StartAsync(
            GetStatusText(state),
            _ => action());
    }

    public Task<T> ShowWhileAsync<T>(
        PromptState state,
        Func<Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return CreateStatus(state).StartAsync(
            GetStatusText(state),
            _ => action());
    }

    public void Show(PromptState state)
    {
        (string symbol, string color) = state switch
        {
            PromptState.Completed => ("\u2713", "green"),
            PromptState.Failed => ("\u2717", "red"),
            _ => ("\u2022", "grey")
        };

        AnsiConsole.MarkupLine($"[{color}]{symbol}[/] {state}");
    }

    private static Status CreateStatus(PromptState state)
    {
        (Spinner spinner, string color) = state switch
        {
            PromptState.Thinking => (Spinner.Known.Dots, "yellow"),
            PromptState.Executing => (Spinner.Known.Star, "blue"),
            _ => throw new ArgumentOutOfRangeException(
                nameof(state),
                state,
                "Only active prompt states can display a spinner.")
        };

        return AnsiConsole.Status()
            .AutoRefresh(true)
            .Spinner(spinner)
            .SpinnerStyle(Style.Parse(color));
    }

    private static string GetStatusText(PromptState state)
    {
        return state switch
        {
            PromptState.Thinking => "Thinking...",
            PromptState.Executing => "Executing...",
            _ => throw new ArgumentOutOfRangeException(
                nameof(state),
                state,
                "Only active prompt states have status text.")
        };
    }
}
