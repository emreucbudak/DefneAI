using DefneAI.Application.DTOs;
using DefneAI.Domain.Enums;

namespace DefneAI.Application.Middleware;

public sealed class RetryMiddleware
{
    public const int DefaultMaxAttempts = 3;

    private readonly int maxAttempts;

    public RetryMiddleware(int maxAttempts = DefaultMaxAttempts)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);
        this.maxAttempts = maxAttempts;
    }

    public async Task<ExecutionAttemptResult> ExecuteAsync(
        ExecutionAttemptContext context,
        Func<ExecutionAttemptContext, CancellationToken, Task<string>> next,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Prompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(context.ExecutionPrompt);
        ArgumentNullException.ThrowIfNull(next);

        string? previousOutput = context.PreviousOutput;
        string? failureReason = context.FailureReason;

        for (int attemptNumber = 1; attemptNumber <= maxAttempts; attemptNumber++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ExecutionAttemptContext attemptContext = context with
            {
                AttemptNumber = attemptNumber,
                ExecutionPrompt = attemptNumber == 1
                    ? context.ExecutionPrompt
                    : CreateRetryPrompt(
                        context.ExecutionPrompt,
                        context.ExecutionLevel,
                        previousOutput,
                        failureReason),
                PreviousOutput = previousOutput,
                FailureReason = failureReason
            };

            try
            {
                string output = await next(attemptContext, cancellationToken);
                if (!string.IsNullOrWhiteSpace(output))
                {
                    return new ExecutionAttemptResult(
                        Success: true,
                        Output: output.Trim(),
                        FailureReason: null,
                        AttemptCount: attemptNumber);
                }

                previousOutput = output;
                failureReason = "AI modeli boş bir sonuç üretti.";
            }
            catch (Exception exception) when (
                IsRetryable(exception, cancellationToken))
            {
                previousOutput = null;
                failureReason = exception.Message;
            }

            if (attemptNumber < maxAttempts)
            {
                await Task.Delay(
                    GetRetryDelay(attemptNumber),
                    cancellationToken);
            }
        }

        return new ExecutionAttemptResult(
            Success: false,
            Output: previousOutput,
            FailureReason: failureReason ?? "Execution başarısız oldu.",
            AttemptCount: maxAttempts);
    }

    private static bool IsRetryable(
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException)
        {
            return !cancellationToken.IsCancellationRequested;
        }

        if (exception is TimeoutException or IOException)
        {
            return true;
        }

        if (exception is not HttpRequestException httpException)
        {
            return false;
        }

        if (httpException.StatusCode is null)
        {
            return true;
        }

        int statusCode = (int)httpException.StatusCode.Value;
        return statusCode is 408 or 429 or >= 500;
    }

    private static TimeSpan GetRetryDelay(int attemptNumber)
    {
        double delayMilliseconds = 250 * Math.Pow(2, attemptNumber - 1);
        return TimeSpan.FromMilliseconds(delayMilliseconds);
    }

    private static string CreateRetryPrompt(
        string originalExecutionPrompt,
        PromptLevel executionLevel,
        string? previousOutput,
        string? failureReason)
    {
        return $"""
            The previous {executionLevel} execution attempt failed.
            Correct the failure and retry the same task.
            Do not repeat side effects that were already completed.

            Original execution request:
            {originalExecutionPrompt}

            Previous output:
            {previousOutput ?? "No output was produced."}

            Failure reason:
            {failureReason ?? "No failure reason was provided."}
            """;
    }
}
