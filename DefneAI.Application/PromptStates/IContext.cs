using DefneAI.Domain.Enums;

namespace DefneAI.Application.PromptStates;

public interface IContext
{
    PromptStateBase State { get; }

    void ChangeState(PromptState state);
}
