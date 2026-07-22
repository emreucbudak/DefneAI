using DefneAI.Domain.Enums;

namespace DefneAI.Application.PromptStates;

public abstract class PromptStateBase
{
    public abstract Task WriteAsync(Func<Task>? action = null);

    public void TransitionTo(
        IContext context,
        PromptState state)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.ChangeState(state);
    }
}
