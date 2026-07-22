using DefneAI.Domain.Enums;

namespace DefneAI.Application.PromptStatus;

public interface IPromptStatusPresenter
{
    Task ShowWhileAsync(
        PromptState state,
        Func<Task> action);

    Task<T> ShowWhileAsync<T>(
        PromptState state,
        Func<Task<T>> action);

    void Show(PromptState state);
}
