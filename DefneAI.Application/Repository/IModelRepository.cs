using DefneAI.Domain.Models;

namespace DefneAI.Application.Repository
{
    public interface IModelRepository
    {
        Task<string> AddModel(AIModelProvider provide);
        Task<string> RemoveModel(string ModelName);
        Task<string> UpdateModel(AIModelProvider provide);
        Task<IEnumerable<AIModelProvider>> GetAllModelProviders();
    }
}
