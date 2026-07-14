using DefneAI.Application.KernelFactory;
using DefneAI.Application.Repository;
using DefneAI.Domain.Models;
using DefneAI.Persistence.Db;
using Microsoft.EntityFrameworkCore;

namespace DefneAI.Persistence.Repository
{
    public sealed class ModelRepository(ModelDbContext context, IKernelFactory kernelFactory) : IModelRepository
    {
        private DbSet<AIModelProvider> Providers => context.Set<AIModelProvider>();

        public async Task<string> AddModel(AIModelProvider provide)
        {
            await Providers.AddAsync(provide);
            await context.SaveChangesAsync();
            kernelFactory.Invalidate();
            return "Model eklendi";
        }

        public async Task<IEnumerable<AIModelProvider>> GetAllModelProviders()
        {
            return await Providers.AsNoTracking().ToListAsync();
        }

        public async Task<string> RemoveModel(string modelName)
        {
            AIModelProvider? model = await Providers.FirstOrDefaultAsync(x => x.ModelName == modelName);
            if (model is null)
            {
                return "Silinmek istenen model bulunamadı";
            }

            model.IsRemoved = true;
            await context.SaveChangesAsync();
            kernelFactory.Invalidate();
            return "Model silindi";
        }

        public async Task<string> UpdateModel(AIModelProvider provide)
        {
            AIModelProvider? model = await Providers.FirstOrDefaultAsync(x => x.Id == provide.Id);
            if (model is null)
            {
                return "Güncellenmek istenen model bulunamadı";
            }

            context.Entry(model).CurrentValues.SetValues(provide);
            await context.SaveChangesAsync();
            kernelFactory.Invalidate();
            return "Model güncellendi";
        }
    }
}
