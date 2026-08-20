using DefneAI.Application.ChatClientFactory;
using DefneAI.Application.InitializerService;
using DefneAI.Application.Repository;
using DefneAI.Domain.Enums;
using DefneAI.Domain.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DefneAI.Infrastructure.InitializerService;

public sealed class ModelInitializerService(
    IModelRepository repo,
    IChatClientFactory chatClientFactory) : IModelInitializerService
{
    public async Task<string> InitializeModelAsync()
    {
        try
        {
            AIModelProvider[] models = await GetActiveModelsAsync();
            chatClientFactory.CreateChatClients(models);
            return $"{models.Length} model çalışmaya hazır.";
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public async Task<IList<HarnessAgent>> GetHarnessAgentsAsync(
        AITaskType taskType)
    {
        AIModelProvider[] models = await GetActiveModelsAsync();
        AIModelProvider[] matchingModels = models
            .Where(model => model.ModelPurpose == taskType)
            .ToArray();
        IReadOnlyDictionary<string, IChatClient> chatClients =
            chatClientFactory.GetCachedChatClients() ??
            chatClientFactory.CreateChatClients(models);
        List<HarnessAgent> modelAgents = new(matchingModels.Length);

        foreach (AIModelProvider model in matchingModels)
        {
            if (!chatClients.TryGetValue(model.ServiceId, out IChatClient? chatClient))
            {
                throw new InvalidOperationException(
                    $"{model.ServiceId} için IChatClient bulunamadı.");
            }

            bool enableWebSearch =
                model.ProviderType == AIProviderType.Gemini &&
                model.ModelPurpose == AITaskType.WebSearch;
            HarnessAgent modelAgent = new(
                chatClient,
                new HarnessAgentOptions
                {
                    Id = model.ServiceId,
                    Name = model.ModelName,
                    Description = model.ModelDescription,
                    HarnessInstructions = string.IsNullOrWhiteSpace(model.ModelSystemPrompt)
                        ? HarnessAgent.DefaultInstructions
                        : model.ModelSystemPrompt,
                    ChatOptions = new ChatOptions
                    {
                        Instructions = model.ModelInstructions,
                        Temperature = (float)model.Temperature
                    },
                    DisableWebSearch = !enableWebSearch
                });

            modelAgents.Add(modelAgent);
        }

        return modelAgents;
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
