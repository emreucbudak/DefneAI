using DefneAI.Application.InitializerService;
using DefneAI.Application.Repository;
using Microsoft.SemanticKernel;

namespace DefneAI.Infrastructure.InitializerService
{
    public class ModelInitializerService(IModelRepository repo,IKernelBuilder kernel) : IModelInitializerService
    {
        public async Task<string> InitializeModelAsync()
        {
            var models = await repo.GetAllModelProviders();
            try
            {
                foreach (var model in models)
                {
                    Uri end = new Uri(model.Endpoint);
                    kernel.AddOpenAIChatCompletion(modelId: model.ModelName, apiKey: model.ApiKey, endpoint: end, serviceId: model.ServiceId);
                }
                return "Modeller çalışmaya hazır.";
            }
            catch (Exception ex) {
                return ex.Message;
            } 
        }
            
        }
    }

