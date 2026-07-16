using DefneAI.Application.KernelFactory;
using DefneAI.Application.Repository;
using DefneAI.Domain.Models;
using DefneAI.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

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

        public async Task<string> UpdateModel(
            string modelName,
            string argumentName,
            string argumentValue)
        {
            AIModelProvider? model = await Providers.FirstOrDefaultAsync(
                provider =>
                    provider.ModelName == modelName ||
                    provider.ModelId == modelName);

            if (model is null)
            {
                return "Güncellenmek istenen model bulunamadı";
            }

            string normalizedArgumentName = argumentName
                .Replace("_", string.Empty, StringComparison.Ordinal)
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .ToLowerInvariant();
            string value = argumentValue.Trim();

            switch (normalizedArgumentName)
            {
                case "modelid":
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        return "ModelId boş olamaz";
                    }
                    model.ModelId = value;
                    break;
                case "modelname":
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        return "ModelName boş olamaz";
                    }
                    model.ModelName = value;
                    break;
                case "modelsystemprompt":
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        return "ModelSystemPrompt boş olamaz";
                    }
                    model.ModelSystemPrompt = value;
                    break;
                case "modeldescription":
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        return "ModelDescription boş olamaz";
                    }
                    model.ModelDescription = value;
                    break;
                case "modelinstructions":
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        return "ModelInstructions boş olamaz";
                    }
                    model.ModelInstructions = value;
                    break;
                case "temperature":
                    if (!double.TryParse(
                            value,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out double temperature) ||
                        temperature is < 0 or > 2)
                    {
                        return "Temperature 0 ile 2 arasında bir sayı olmalıdır";
                    }
                    model.Temperature = temperature;
                    break;
                case "apikey":
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        return "ApiKey boş olamaz";
                    }
                    model.ApiKey = value;
                    break;
                case "endpoint":
                    if (!Uri.TryCreate(value, UriKind.Absolute, out _))
                    {
                        return "Endpoint geçerli bir mutlak adres olmalıdır";
                    }
                    model.Endpoint = value;
                    break;
                case "serviceid":
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        return "ServiceId boş olamaz";
                    }
                    model.ServiceId = value;
                    break;
                case "prioritynumber":
                    if (!int.TryParse(
                            value,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out int priorityNumber))
                    {
                        return "PriorityNumber geçerli bir tam sayı olmalıdır";
                    }
                    model.PriorityNumber = priorityNumber;
                    break;
                case "isremoved":
                    if (!bool.TryParse(value, out bool isRemoved))
                    {
                        return "IsRemoved true veya false olmalıdır";
                    }
                    model.IsRemoved = isRemoved;
                    break;
                default:
                    return $"Güncellenmek istenen model alanı desteklenmiyor: {argumentName}";
            }

            await context.SaveChangesAsync();
            kernelFactory.Invalidate();
            return "Model güncellendi";
        }
    }
}
