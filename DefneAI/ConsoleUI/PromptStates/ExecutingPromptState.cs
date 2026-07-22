using DefneAI.Application.PromptStates;
using Spectre.Console;

namespace DefneAI.ConsoleUI.PromptStates;

public sealed class ExecutingPromptState : PromptStateBase
{
    public override Task WriteAsync(Func<Task>? action = null)
    {
        Func<Task> operation = action ?? (() => Task.CompletedTask);

        return AnsiConsole.Status()
            .AutoRefresh(true)
            .Spinner(Spinner.Known.Star)
            .SpinnerStyle(Style.Parse("blue"))
            .StartAsync(
                "Executing...",
                _ => operation());
    }
}
