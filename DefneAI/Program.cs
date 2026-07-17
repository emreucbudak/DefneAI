using System.Text;
using DefneAI.Application.ExecutionService;
using DefneAI.Application.ActionSecurityLevelService;
using DefneAI.Application.InitializerService;
using DefneAI.Application.KernelFactory;
using DefneAI.Application.PromptIntentService;
using DefneAI.Application.PromptLevelService;
using DefneAI.Application.Repository;
using DefneAI.Application.Router;
using DefneAI.Infrastructure.ActionSecurityLevelService;
using DefneAI.Infrastructure.ExecutionService;
using DefneAI.Infrastructure.InitializerService;
using DefneAI.Infrastructure.KernelFactory;
using DefneAI.Infrastructure.Plugin;
using DefneAI.Infrastructure.PromptIntentService;
using DefneAI.Infrastructure.PromptLevelService;
using DefneAI.Persistence.Db;
using DefneAI.Persistence.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

Console.Title = "DefneAI - The AI Assistant for Developers";
Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;
if (!Console.IsOutputRedirected)
{
    Console.BackgroundColor = ConsoleColor.Black;
    Console.Clear();
}

/*
DefneAI is an AI assistant designed to help developers with various tasks. It can automate application management, provide code suggestions, and assist in debugging. The assistant leverages the power of AI to enhance productivity and streamline development workflows.
*/
ServiceCollection services = new();
services.AddMemoryCache();
services.AddScoped<DefneAI.Application.Commands.ICommandDispatcher,
    DefneAI.Infrastructure.Commands.CommandDispatcher>();
services.AddSingleton<DefneAutomationPlugin>();
services.AddSingleton<IKernelFactory, DynamicKernelFactory>();
services.AddScoped<IModelRepository, ModelRepository>();
services.AddScoped<IChatRepository, ChatRepository>();
services.AddScoped<IPromptRepository, PromptRepository>();
services.AddScoped<IAIResponseRepository, AIResponseRepository>();
services.AddScoped<IModelInitializerService, ModelInitializerService>();
services.AddScoped<IPromptIntentService, PromptIntentService>();
services.AddScoped<IPromptLevelService, PromptLevelService>();
services.AddScoped<IActionSecurityLevelService, ActionSecurityLevelService>();
services.AddScoped<IModelExecutionService, ModelExecutionService>();
services.AddScoped<DefneAgentRouter>();
string? databaseConnection =
    Environment.GetEnvironmentVariable("DEFNEAI_DB_CONNECTION") ??
    Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
bool isDatabaseConfigured = !string.IsNullOrWhiteSpace(databaseConnection);
services.AddDbContext<ModelDbContext>(options =>
{
    if (isDatabaseConfigured)
    {
        options.UseNpgsql(databaseConnection);
    }
});

using ServiceProvider serviceProvider = services.BuildServiceProvider();
using IServiceScope scope = serviceProvider.CreateScope();

IModelInitializerService modelInitializer = scope.ServiceProvider.GetRequiredService<IModelInitializerService>();
DefneAgentRouter defne = scope.ServiceProvider.GetRequiredService<DefneAgentRouter>();

if (isDatabaseConfigured)
{
    Console.WriteLine(await modelInitializer.InitializeModelAsync());
}
else
{
    Console.WriteLine(
        "Model veritabani yapilandirilmadi; Gemma beyin DB olmadan calisiyor. " +
        "Model komutlari icin DEFNEAI_DB_CONNECTION ayarla.");
}

while (true)
{
    Console.Write("Emre: ");
    string? userInput = Console.ReadLine();
    if (userInput is null)
    {
        break;
    }

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
