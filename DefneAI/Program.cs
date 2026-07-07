using Microsoft.SemanticKernel;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using DefneAI.Application.Router;

Console.Title= "DefneAI - The AI Assistant for Developers";
Console.InputEncoding = System.Text.Encoding.UTF8;
Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.BackgroundColor = ConsoleColor.Black; 
Console.Clear();
var builder = Kernel.CreateBuilder();
builder.Plugins.AddFromType<DefneAI.Infrastructure.Plugin.DefneAutomationPlugin>();
builder.AddOpenAIChatCompletion("gemma4:e4b", apiKey: "ollama", endpoint: new Uri("http://localhost:11434/v1"),serviceId:"DefneAI");
builder.AddOpenAIChatCompletion("qwen2.5-coder:7b", apiKey: "ollama", endpoint: new Uri("http://localhost:11434/v1"),serviceId:"Qwen2.5-Coder");
Kernel kernel = builder.Build();
/*
DefneAI is an AI assistant designed to help developers with various tasks. It can automate application management, provide code suggestions, and assist in debugging. The assistant leverages the power of AI to enhance productivity and streamline development workflows.
*/
builder.Services.AddSingleton<DefneAI.Application.Router.DefneAgentRouter>();
DefneAgentRouter defne = new DefneAgentRouter(kernel);
while (true)
{
    Console.Write("Emre: ");
    string userInput = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(userInput))
    {
        continue;
    }


    StringBuilder stringBuilder = new StringBuilder();
    stringBuilder.Append("Düşünüyor... \n");
    Console.Write(stringBuilder.ToString());
    var response = await defne.GetPromptResult(userInput);
    Console.WriteLine($"Defne: {response}");




    Console.WriteLine();
}