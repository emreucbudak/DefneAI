using DefneAI.Domain.Models;

namespace DefneAI.Application.Repository
{
    public interface IModelRepository
    {
        Task<string> AddModel(AIModelProvider provide);
        Task<string> RemoveModel(string ModelName);
        Task<string> UpdateModel(
            string modelName,
            string argumentName,
            string argumentValue);
        Task<IEnumerable<AIModelProvider>> GetAllModelProviders();
    }
}
