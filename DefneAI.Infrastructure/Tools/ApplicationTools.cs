using System.ComponentModel;
using System.Diagnostics;

namespace DefneAI.Infrastructure.Tools;

public sealed class ApplicationTools
{
    [Description("Bu Fonksiyon istenen uygulamayı açar")]
    public string OpenApplication(string applicationName)
    {
        try
        {
            var process = new Process();
            process.StartInfo.FileName = applicationName;
            process.Start();
            return $"Uygulama '{applicationName}' başarıyla açıldı.";
        }
        catch (Exception ex)
        {
            return $"Uygulama '{applicationName}' açılamadı: {ex.Message}";
        }
    }

    [Description("Bu Fonksiyon istenen uygulamayı kapatır")]
    public string CloseApplication(string applicationName)
    {
        try
        {
            var processes = Process.GetProcessesByName(applicationName);
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

    [Description("Bu Fonksiyon istenen uygulamanın durumunu kontrol eder")]
    public string CheckApplicationStatus(string applicationName)
    {
        try
        {
            var processes = Process.GetProcessesByName(applicationName);
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
}
