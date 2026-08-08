namespace DefneAI.Application.Execution;

public interface IExecutionOrchestrator
{
    Task<string> ExecuteAsync(
        ExecutionRequest request,
        CancellationToken cancellationToken = default);
}
