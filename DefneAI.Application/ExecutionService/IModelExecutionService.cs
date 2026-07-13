namespace DefneAI.Application.ExecutionService
{
    public interface IModelExecutionService
    {
        Task<string> GetPromptResult();
    }
}
