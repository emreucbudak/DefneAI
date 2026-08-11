using DefneAI.Application.DTOs;
using DefneAI.Domain.Models;

namespace DefneAI.Application.ModelFactory;

public interface IModelProviderFactory
{
    AIModelProvider Create(AddModelDto model);
}
