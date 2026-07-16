using DefneAI.Application.InitializerService;
using DefneAI.Application.KernelFactory;
using DefneAI.Application.Repository;
using DefneAI.Domain.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace DefneAI.Infrastructure.InitializerService
{
    public sealed class ModelInitializerService(IModelRepository repo, IKernelFactory kernelFactory) : IModelInitializerService
    {
        private const string CLIBrainModelId = "gemma4:e4b";
        private const string CLIBrainServiceId = "defne-cli-brain";
        private const string CLIBrainEndpoint = "http://localhost:11434/v1";
        private ChatCompletionAgent? cliBrain;

        public async Task<string> InitializeModelAsync()
        {
            try
            {
                AIModelProvider[] models = await GetActiveModelsAsync();
                kernelFactory.CreateKernel(models);
                return $"{models.Length} model çalışmaya hazır.";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<Kernel> GetKernelAsync()
        {
            Kernel? cachedKernel = kernelFactory.GetCachedKernel();
            if (cachedKernel is not null)
            {
                return cachedKernel;
            }

            AIModelProvider[] models = await GetActiveModelsAsync();
            return kernelFactory.CreateKernel(models);
        }

        public async Task<IList<ChatCompletionAgent>> GetChatCompletionAgentsAsync()
        {
            AIModelProvider[] models = await GetActiveModelsAsync();
            Kernel kernel = kernelFactory.GetCachedKernel() ?? kernelFactory.CreateKernel(models);
            List<ChatCompletionAgent> modelAgents = new(models.Length);

            foreach (AIModelProvider model in models)
            {
                OpenAIPromptExecutionSettings prompt = new()
                {
                    ServiceId = model.ServiceId,
                    Temperature = model.Temperature,
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                };

                ChatCompletionAgent modelAgent = new()
                {
                    Name = model.ModelName,
                    Description = model.ModelDescription,
                    Kernel = kernel,
                    Arguments = new KernelArguments(prompt),
                    Instructions = model.ModelInstructions
                };

                modelAgents.Add(modelAgent);
            }

            return modelAgents;
        }

        public ChatCompletionAgent GetCLIBrain()
        {
            if (cliBrain is not null)
            {
                return cliBrain;
            }

            IKernelBuilder builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion(
                modelId: CLIBrainModelId,
                apiKey: "ollama",
                endpoint: new Uri(CLIBrainEndpoint, UriKind.Absolute),
                serviceId: CLIBrainServiceId);

            Kernel brainKernel = builder.Build();
            OpenAIPromptExecutionSettings settings = new()
            {
                ServiceId = CLIBrainServiceId,
                Temperature = 0,
                ResponseFormat = "json_object"
            };

            cliBrain = new ChatCompletionAgent
            {
                Name = "DefneCLIBrain",
                Description = $"Local Ollama CLI brain: {CLIBrainModelId}",
                Kernel = brainKernel,
                Arguments = new KernelArguments(settings),
                Instructions =
                    "Analyze the user's prompt and return only a JSON object with intent and level. " +
                    "Allowed intent values: Coding, OfficeTask, WebSearch, GeneralChat. " +
                    "Allowed level values: LOW, MEDIUM, HIGH, EXTRAHIGH. " +
                    "Do not add markdown or explanations."
            };

            return cliBrain;
        }

        private async Task<AIModelProvider[]> GetActiveModelsAsync()
        {
            IEnumerable<AIModelProvider> models = await repo.GetAllModelProviders();
            return models
                .Where(model => !model.IsRemoved)
                .OrderBy(model => model.PriorityNumber)
                .ThenBy(model => model.Id)
                .ToArray();
        }
    }
}
