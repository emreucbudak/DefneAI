using DefneAI.Application.PromptStates;
using DefneAI.Domain.Enums;

namespace DefneAI.ConsoleUI.PromptStates;

public sealed class PromptStateContext : IContext
{
    public PromptStateContext()
    {
        State = PromptStateFactory.Create(PromptState.Thinking);
    }

    public PromptStateBase State { get; private set; }

    public void ChangeState(PromptState state) =>
        State = PromptStateFactory.Create(state);
}
