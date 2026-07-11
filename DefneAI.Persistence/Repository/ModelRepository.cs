using DefneAI.Application.Repository;
using DefneAI.Domain.Models;
using DefneAI.Persistence.Db;
using Microsoft.EntityFrameworkCore;

namespace DefneAI.Persistence.Repository
{
    public class ModelRepository(ModelDbContext context) : IModelRepository
    {
        private DbSet<AIModelProvider> providers => context.Set<AIModelProvider>();
        public async Task<string> AddModel(AIModelProvider provide)
        {
            await providers.AddAsync(provide);
            return "Model Eklendi";
        }

        public async Task<IEnumerable<AIModelProvider>> GetAllModelProviders()
        {
            return await providers.AsNoTracking().ToListAsync();
        }

        public async Task<string> RemoveModel(string ModelName)
        {
            var model = await providers.Where(x=> x.ModelName == ModelName).FirstOrDefaultAsync();
            if (model is null)
            {
                return "Silinmek istenen model bulunamadı";
            }
            providers.Remove(model);
            return "Model silindi";
        }

        public Task<string> UpdateModel(AIModelProvider provide)
        {
            throw new NotImplementedException();
        }
    }
}
