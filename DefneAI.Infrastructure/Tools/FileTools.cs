using System.ComponentModel;
using System.Diagnostics;

namespace DefneAI.Infrastructure.Tools;

public sealed class FileTools
{
    [Description("Bu Fonksiyon belirtilen dosya uzantısı veya web linkini açar")]
    public string OpenFileOrLink(string pathOrUrl)
    {
        try
        {
            var process = new Process();
            process.StartInfo.FileName = pathOrUrl;
            process.Start();
            return $"Dosya veya link '{pathOrUrl}' başarıyla açıldı.";
        }
        catch (Exception ex)
        {
            return $"Dosya veya link '{pathOrUrl}' açılamadı: {ex.Message}";
        }
    }

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
}
