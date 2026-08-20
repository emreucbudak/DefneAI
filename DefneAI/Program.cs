using System.Text;
using DefneAI.Application.DTOs;
using DefneAI.Application.Execution;
using DefneAI.Application.PromptAnalysis;
using DefneAI.Application.ChatSession;
using DefneAI.Application.InitializerService;
using DefneAI.Application.ChatClientFactory;
using DefneAI.Application.ModelFactory;
using DefneAI.Application.Repository;
using DefneAI.Application.Router;
using DefneAI.Application.PromptStates;
using DefneAI.Application.Validators;
using DefneAI.ConsoleUI;
using DefneAI.ConsoleUI.PromptStates;
using DefneAI.Infrastructure.ChatSession;
using DefneAI.Infrastructure.ExecutionService;
using DefneAI.Infrastructure.InitializerService;
using DefneAI.Infrastructure.ChatClientFactory;
using DefneAI.Infrastructure.ModelFactory;
using DefneAI.Infrastructure.Tools;
using DefneAI.Persistence.Db;
using DefneAI.Persistence.Repository;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Spectre.Console;

Console.Title = "DefneAI - The AI Assistant for Developers";
Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;
if (!Console.IsOutputRedirected)
{
    Console.BackgroundColor = ConsoleColor.Black;
    Console.Clear();
}

using ConsoleChatUi consoleUi = new();

/*
DefneAI is an AI assistant designed to help developers with various tasks. It can automate application management, provide code suggestions, and assist in debugging. The assistant leverages the power of AI to enhance productivity and streamline development workflows.
*/
ServiceCollection services = new();
services.AddMemoryCache();

const string cliBrainModelId = "gemma4:e4b";
const string cliBrainServiceId = "defne-cli-brain";
IKernelBuilder cliBrainBuilder = Kernel.CreateBuilder();
cliBrainBuilder.AddOpenAIChatCompletion(
    modelId: cliBrainModelId,
    apiKey: "ollama",
    endpoint: new Uri("http://localhost:11434/v1", UriKind.Absolute),
    serviceId: cliBrainServiceId);
Kernel cliBrainKernel = cliBrainBuilder.Build();
OpenAIPromptExecutionSettings cliBrainSettings = new()
{
    ServiceId = cliBrainServiceId,
    Temperature = 0
};
ChatCompletionAgent cliBrain = new()
{
    Name = "DefneCLIBrain",
    Description = $"Local Ollama CLI brain: {cliBrainModelId}",
    Kernel = cliBrainKernel,
    Arguments = new KernelArguments(cliBrainSettings),
    Instructions =
        "Classify the user's prompt according to the criteria supplied in each request. " +
        "Return only the single value requested by the criteria. " +
        "Do not add JSON, quotes, markdown, or explanations."
};
services.AddSingleton(cliBrain);
services.AddScoped<DefneAI.Application.Commands.ICommandDispatcher,
    DefneAI.Infrastructure.Commands.CommandDispatcher>();
services.AddSingleton<ApplicationTools>();
services.AddSingleton<CommandTools>();
services.AddSingleton<FileTools>();
services.AddSingleton<ModelTools>();
services.AddSingleton<WebTools>();
services.AddSingleton<IChatClientFactory, DynamicChatClientFactory>();
services.AddSingleton<IValidator<AddModelDto>, AddModelDtoValidator>();
services.AddSingleton<IModelProviderFactory, ModelProviderFactory>();
services.AddScoped<IModelRepository, ModelRepository>();
services.AddScoped<IChatRepository, ChatRepository>();
services.AddScoped<IPromptRepository, PromptRepository>();
services.AddScoped<IAIResponseRepository, AIResponseRepository>();
services.AddSingleton<IChatSessionService, ChatSessionService>();
services.AddScoped<IModelInitializerService, ModelInitializerService>();
services.AddScoped<IPromptAnalysisService, PromptAnalysisService>();
services.AddScoped<IExecutionService, ExecutionService>();
services.AddScoped<IContext, PromptStateContext>();
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
    string userInput = consoleUi.ReadPrompt();

    if (string.IsNullOrWhiteSpace(userInput))
    {
        continue;
    }

    string response = await defne.GetPromptResult(userInput);
    AnsiConsole.Markup("[bold green]Defne:[/] ");
    AnsiConsole.WriteLine(response);
    AnsiConsole.WriteLine();
}
