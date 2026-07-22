using DefneAI.Application.PromptStates;
using Spectre.Console;

namespace DefneAI.ConsoleUI.PromptStates;

public sealed class FailedPromptState : PromptStateBase
{
    public override async Task WriteAsync(Func<Task>? action = null)
    {
        if (action is not null)
        {
            await action();
        }

        AnsiConsole.MarkupLine("[red]\u2717[/] Failed");
    }
}
