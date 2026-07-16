using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace DefneAI.Application.InitializerService
{
    public interface IModelInitializerService
    {
        Task<string> InitializeModelAsync();
        Task<Kernel> GetKernelAsync();
        Task<IList<ChatCompletionAgent>> GetChatCompletionAgentsAsync();
        ChatCompletionAgent GetCLIBrain();
    }
}
