using DefneAI.Application.PromptStates;
using DefneAI.Domain.Enums;

namespace DefneAI.ConsoleUI.PromptStates;

public static class PromptStateFactory
{
    public static PromptStateBase Create(PromptState state)
    {
        return state switch
        {
            PromptState.Thinking => new ThinkingPromptState(),
            PromptState.Executing => new ExecutingPromptState(),
            PromptState.Completed => new CompletedPromptState(),
            PromptState.Failed => new FailedPromptState(),
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
    }
}
