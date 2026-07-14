using DefneAI.Domain.Models;
using Microsoft.SemanticKernel;

namespace DefneAI.Application.KernelFactory
{
    public interface IKernelFactory
    {
        Kernel CreateKernel(IReadOnlyCollection<AIModelProvider> models);
        Kernel? GetCachedKernel();
        void Invalidate();
    }
}
