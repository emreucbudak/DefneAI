using System.Text;
using DefneAI.Application.ExecutionService;
using DefneAI.Application.InitializerService;
using DefneAI.Application.KernelFactory;
using DefneAI.Application.Repository;
using DefneAI.Application.Router;
using DefneAI.Infrastructure.ExecutionService;
using DefneAI.Infrastructure.InitializerService;
using DefneAI.Infrastructure.KernelFactory;
using DefneAI.Infrastructure.Plugin;
using DefneAI.Persistence.Db;
using DefneAI.Persistence.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

Console.Title = "DefneAI - The AI Assistant for Developers";
Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;
Console.BackgroundColor = ConsoleColor.Black;
Console.Clear();

/*
DefneAI is an AI assistant designed to help developers with various tasks. It can automate application management, provide code suggestions, and assist in debugging. The assistant leverages the power of AI to enhance productivity and streamline development workflows.
*/
ServiceCollection services = new();
services.AddMemoryCache();
services.AddSingleton<DefneAutomationPlugin>();
services.AddSingleton<IKernelFactory, DynamicKernelFactory>();
services.AddScoped<IModelRepository, ModelRepository>();
services.AddScoped<IModelInitializerService, ModelInitializerService>();
services.AddScoped<IModelExecutionService, ModelExecutionService>();
services.AddScoped<DefneAgentRouter>();
services.AddDbContext<ModelDbContext>(options =>
{
    options.UseNpgsql("DefaultConnection");
});

using ServiceProvider serviceProvider = services.BuildServiceProvider();
using IServiceScope scope = serviceProvider.CreateScope();

IModelInitializerService modelInitializer = scope.ServiceProvider.GetRequiredService<IModelInitializerService>();
DefneAgentRouter defne = scope.ServiceProvider.GetRequiredService<DefneAgentRouter>();

Console.WriteLine(await modelInitializer.InitializeModelAsync());

while (true)
{
    Console.Write("Emre: ");
    string? userInput = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(userInput))
    {
        continue;
    }

    StringBuilder stringBuilder = new();
    stringBuilder.Append("Düşünüyor... \n");
    Console.Write(stringBuilder.ToString());

    string response = await defne.GetPromptResult(userInput);
    Console.WriteLine($"Defne: {response}");
    Console.WriteLine();
}
