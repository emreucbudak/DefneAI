using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.InitializerService
{
    public interface IModelInitializerService
    {
        Task<string> InitializeModelAsync();
        Task<IList<ChatCompletionAgent>> GetChatCompletionAgentsAsync();
    }
}
