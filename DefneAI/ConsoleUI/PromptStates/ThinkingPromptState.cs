using DefneAI.Application.PromptStates;
using Spectre.Console;

namespace DefneAI.ConsoleUI.PromptStates;

public sealed class ThinkingPromptState : PromptStateBase
{
    public override Task WriteAsync(Func<Task>? action = null)
    {
        Func<Task> operation = action ?? (() => Task.CompletedTask);

        return AnsiConsole.Status()
            .AutoRefresh(true)
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("yellow"))
            .StartAsync(
                "Thinking...",
                _ => operation());
    }
}
