using System.ComponentModel;
using System.Diagnostics;
using DefneAI.Application.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace DefneAI.Infrastructure.Tools;

public sealed class CommandTools(IServiceScopeFactory scopeFactory)
{
    [Description("DefneAI komutunu CommandDispatcher üzerinden çalıştırır")]
    public Task<string> ExecuteCommand(string command)
    {
        return DispatchCommandAsync(command);
    }

    [Description("Bu Fonksiyon istenen komudu PowerShell'de çalıştırır")]
    public string ExecutePowerShellCommand(string command)
    {
        try
        {
            var process = new Process();
            process.StartInfo.FileName = "powershell.exe";
            process.StartInfo.Arguments = $"-Command \"{command}\"";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return $"PowerShell komutu başarıyla çalıştırıldı. Çıktı:\n{output}";
        }
        catch (Exception ex)
        {
            return $"PowerShell komutu çalıştırılamadı: {ex.Message}";
        }
    }

    private async Task<string> DispatchCommandAsync(
        string command,
        CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        ICommandDispatcher commandDispatcher =
            scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        return await commandDispatcher.ExecuteAsync(
            command,
            cancellationToken);
    }
}
