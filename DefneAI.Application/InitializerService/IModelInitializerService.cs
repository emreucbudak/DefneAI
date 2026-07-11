namespace DefneAI.Application.InitializerService
{
    public interface IModelInitializerService
    {
        Task<string> InitializeModelAsync();
    }
}
