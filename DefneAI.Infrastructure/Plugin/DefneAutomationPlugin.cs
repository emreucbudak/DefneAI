using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace DefneAI.Infrastructure.Plugin
{
    public sealed partial class DefneAutomationPlugin(IServiceScopeFactory scopeFactory)
    {
        private readonly IServiceScopeFactory scopeFactory = scopeFactory;

        [KernelFunction]
        [Description("Bu Fonksiyon istenen uygulamayı açar")]
        public string OpenApplication(string applicationName)
        {
            try
            {
                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = applicationName;
                process.Start();
                return $"Uygulama '{applicationName}' başarıyla açıldı.";
            }
            catch (Exception ex)
            {
                return $"Uygulama '{applicationName}' açılamadı: {ex.Message}";
            }
        }
        [KernelFunction]
        [Description("Bu Fonksiyon istenen uygulamayı kapatır")]
        public string CloseApplication(string applicationName)
        {
            try
            {
                var processes = System.Diagnostics.Process.GetProcessesByName(applicationName);
                foreach (var process in processes)
                {
                    process.Kill();
                }
                return $"Uygulama '{applicationName}' başarıyla kapatıldı.";
            }
            catch (Exception ex)
            {
                return $"Uygulama '{applicationName}' kapatılamadı: {ex.Message}";
            }
        }
        [KernelFunction]
        [Description("Bu Fonksiyon istenen uygulamanın durumunu kontrol eder")]
        public string CheckApplicationStatus(string applicationName)
        {
            try
            {
                var processes = System.Diagnostics.Process.GetProcessesByName(applicationName);
                if (processes.Length > 0)
                {
                    return $"Uygulama '{applicationName}' çalışıyor.";
                }
                else
                {
                    return $"Uygulama '{applicationName}' çalışmıyor.";
                }
            }
            catch (Exception ex)
            {
                return $"Uygulama '{applicationName}' durumu kontrol edilemedi: {ex.Message}";
            }
        }
        [KernelFunction]
        [Description("Bu Fonksiyon belirtilen dosya uzantısı veya web linkini açar")]
        public string OpenFileOrLink(string pathOrUrl)
        {
            try
            {
                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = pathOrUrl;
                process.Start();
                return $"Dosya veya link '{pathOrUrl}' başarıyla açıldı.";
            }
            catch (Exception ex)
            {
                return $"Dosya veya link '{pathOrUrl}' açılamadı: {ex.Message}";
            }
        }
        [KernelFunction]
        [Description("Bu fonksiyon gmaili açar ve son mailleri çeker ")]
        public string OpenGmailAndFetchEmails()
        {
            try
            {
                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "https://mail.google.com/";
                process.Start();
                return "Gmail başarıyla açıldı ve son mailler çekildi.";
            }
            catch (Exception ex)
            {
                return $"Gmail açılamadı: {ex.Message}";
            }
        }
        [KernelFunction]
        [Description("Bu Fonksiyon istenen dosyanın içeriğini değiştirir")]
        public async Task<string> ModifyFileContent(string filePath, string newContent)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return $"Dosya '{filePath}' bulunamadı.";
                }
                await File.WriteAllTextAsync(filePath, newContent);
                return $"Dosya '{filePath}' başarıyla güncellendi.";

            }
            catch (Exception ex)
            {
                return $"Dosya '{filePath}' güncellenemedi: {ex.Message}";
            }
        }
        [KernelFunction]
        [Description("Bu Fonksiyon istenen dosyanın içeriğini okur")]
        public async Task<string> ReadFileContent(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return $"Dosya '{filePath}' bulunamadı.";
                }
                string content = await File.ReadAllTextAsync(filePath);
                return content;
            }
            catch (Exception ex)
            {
                return $"Dosya '{filePath}' okunamadı: {ex.Message}";
            }
        }
        [KernelFunction]
        [Description("Bu Fonksiyon istenen dosyayı siler")]
        public async Task<string> DeleteFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return $"Dosya '{filePath}' bulunamadı.";
                }
                File.Delete(filePath);
                return $"Dosya '{filePath}' başarıyla silindi.";
            }
            catch (Exception ex)
            {
                return $"Dosya '{filePath}' silinemedi: {ex.Message}";
            }
        }
        [KernelFunction]
        [Description("Bu Fonksiyon Cmdde istenen fonksiyonu çalıştırır")]
        public string ExecuteCommand(string command)
        {
            try
            {
                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"/C {command}";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return $"Komut başarıyla çalıştırıldı. Çıktı:\n{output}";
            }
            catch (Exception ex)
            {
                return $"Komut çalıştırılamadı: {ex.Message}";
            }
        }
        [KernelFunction]
        [Description("Bu Fonksiyon istenen komudu PowerShell'de çalıştırır")]
        public string ExecutePowerShellCommand(string command)
        {
            try
            {
                var process = new System.Diagnostics.Process();
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
    }
}