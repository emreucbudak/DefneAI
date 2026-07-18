using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using DefneAI.Domain.Enums;

namespace DefneAI.Application.InitializerService
{
    public interface IModelInitializerService
    {
        Task<string> InitializeModelAsync();
        Task<Kernel> GetKernelAsync();
        Task<IList<ChatCompletionAgent>> GetChatCompletionAgentsAsync(AITaskType taskType);
        ChatCompletionAgent GetCLIBrain();
    }
}
