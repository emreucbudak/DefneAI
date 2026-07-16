using DefneAI.Application.DTOs;

namespace DefneAI.Application.Commands
{
    public interface ICommandDispatcher
    {
        bool IsCommand(string input);

        Task<string> AddModelAsync(
            AddModelDto model,
            CancellationToken cancellationToken = default);

        Task<string> ExecuteAsync(
            string input,
            CancellationToken cancellationToken = default);
    }
}
