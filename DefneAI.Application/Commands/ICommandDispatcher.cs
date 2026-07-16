namespace DefneAI.Application.Commands
{
    public interface ICommandDispatcher
    {
        bool IsCommand(string input);

        Task<string> ExecuteAsync(
            string input,
            CancellationToken cancellationToken = default);
    }
}
