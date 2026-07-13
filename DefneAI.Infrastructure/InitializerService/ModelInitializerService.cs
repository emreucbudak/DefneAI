using DefneAI.Application.InitializerService;
using DefneAI.Application.Repository;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace DefneAI.Infrastructure.InitializerService
{
    public class ModelInitializerService(IModelRepository repo, IKernelBuilder kernel) : IModelInitializerService
    {
        public async Task<string> InitializeModelAsync()
        {
            var models = await repo.GetAllModelProviders();
            try
            {
                foreach (var model in models)
                {
                    Uri end = new Uri(model.Endpoint);
                    kernel.AddOpenAIChatCompletion(modelId: model.ModelId, apiKey: model.ApiKey, endpoint: end, serviceId: model.ServiceId);
                }
                return "Modeller çalışmaya hazır.";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public async Task<IList<ChatCompletionAgent>> GetChatCompletionAgentsAsync()
        {
            var models = await repo.GetAllModelProviders();
            var modelAgents = new List<ChatCompletionAgent>();
            Kernel kern = kernel.Build();
            OpenAIPromptExecutionSettings prompt = new OpenAIPromptExecutionSettings() {
                Temperature = 0.5,
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                
            };

            try
            {
                foreach(var model in models)
                {
                    prompt.ServiceId = model.ServiceId;
                    var modelAgent = new ChatCompletionAgent()
                    {
                        Name = model.ModelName,
                        Description = model.ModelDescription,
                        Kernel = kern,
                        Arguments = new KernelArguments(prompt),
                        Instructions = model.ModelInstructions
                    };
                    modelAgents.Add(modelAgent);
                }
                return modelAgents;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}

