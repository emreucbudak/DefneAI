using DefneAI.Domain.Enums;
using Microsoft.Agents.AI;

namespace DefneAI.Application.InitializerService;

public interface IModelInitializerService
{
    Task<string> InitializeModelAsync();

    Task<IList<HarnessAgent>> GetHarnessAgentsAsync(AITaskType taskType);
}
