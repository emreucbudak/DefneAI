using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using DefneAI.Application.Commands;
using DefneAI.Application.DTOs;
using DefneAI.Domain.Enums;
using System.Diagnostics;
using YoutubeExplode;
using YoutubeExplode.Search;
using YoutubeExplode.Videos;

namespace DefneAI.Infrastructure.Plugin
{
    public sealed class DefneAutomationPlugin(IServiceScopeFactory scopeFactory)
    {
        private readonly IServiceScopeFactory scopeFactory = scopeFactory;
        private const int YouTubeSearchResultLimit = 8;

        [KernelFunction]
        [Description("Verilen HTTP veya HTTPS linkini varsayilan web tarayicisinda acar. Yalnizca sayfayi acar; tarayici kontrolu yapmaz.")]
        public string OpenWebLink(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
                (uri.Scheme != Uri.UriSchemeHttp &&
                 uri.Scheme != Uri.UriSchemeHttps))
            {
                return "Gecerli bir HTTP veya HTTPS linki belirtilmelidir.";
            }

            return OpenUrlInDefaultBrowser(
                uri.AbsoluteUri,
                $"Link '{uri.AbsoluteUri}'");
        }

        [KernelFunction]
        [Description("YouTube'da video adiyla arama yapar ve secim icin numarali video listesi dondurur. Kullanici video istediginde once bu fonksiyonu cagir, listeyi goster ve secimini sor. Bu fonksiyon video acmaz.")]
        public async Task<string> SearchYouTubeVideos(
            string videoName,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(videoName))
            {
                return "YouTube video adi belirtilmelidir.";
            }

            try
            {
                List<VideoSearchResult> videos = [];
                using YoutubeClient youtubeClient = new();

                await foreach (VideoSearchResult video in
                               youtubeClient.Search.GetVideosAsync(
                                   videoName,
                                   cancellationToken))
                {
                    videos.Add(video);

                    if (videos.Count >= YouTubeSearchResultLimit)
                    {
                        break;
                    }
                }

                if (videos.Count == 0)
                {
                    return $"'{videoName}' icin YouTube videosu bulunamadi.";
                }

                List<string> resultLines =
                [
                    $"YouTube'da '{videoName}' icin bulunan videolar:"
                ];

                for (int index = 0; index < videos.Count; index++)
                {
                    VideoSearchResult video = videos[index];
                    resultLines.Add(
                        $"{index + 1}. {ToSingleLine(video.Title)}");
                    resultLines.Add(
                        $"   Kanal: {ToSingleLine(video.Author.ChannelTitle)}");
                    resultLines.Add(
                        $"   Sure: {FormatYouTubeDuration(video.Duration)}");
                    resultLines.Add(
                        $"   URL: {video.Url}");
                }

                resultLines.Add(
                    $"Kullanicidan 1 ile {videos.Count} arasinda bir video secmesini iste. Secim yapilmadan video acma.");

                return string.Join(Environment.NewLine, resultLines);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                return "YouTube video aramasi iptal edildi.";
            }
            catch (Exception ex)
            {
                return $"YouTube video listesi alinamadi: {ex.Message}";
            }
        }

        [KernelFunction]
        [Description("Yalnizca kullanici SearchYouTubeVideos listesinden bir video sectikten sonra secilen videonun URL veya kimligini varsayilan tarayicida acar. Video adi aramak icin kullanma; secim olmadan cagirma.")]
        public string OpenYouTubeVideo(string videoUrlOrId)
        {
            if (string.IsNullOrWhiteSpace(videoUrlOrId))
            {
                return "YouTube video linki veya kimligi belirtilmelidir.";
            }

            if (VideoId.TryParse(videoUrlOrId) is not { } videoId)
            {
                return "Gecerli bir YouTube video linki veya kimligi belirtilmelidir. Video aramak icin once SearchYouTubeVideos kullanilmalidir.";
            }

            string videoUrl =
                $"https://www.youtube.com/watch?v={videoId}";

            return OpenUrlInDefaultBrowser(
                videoUrl,
                "YouTube videosu");
        }

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
        [Description("Kayıtlı AI modellerini API anahtarlarını göstermeden listeler")]
        public Task<string> ListModels()
        {
            return DispatchCommandAsync("/modellistele");
        }

        [KernelFunction]
        [Description("Yeni bir AI modeli kaydeder")]
        public Task<string> AddModel(
            string modelId,
            string modelName,
            string modelSystemPrompt,
            string modelDescription,
            string modelInstructions,
            AITaskType modelPurpose,
            double temperature,
            string apiKey,
            string endpoint,
            string serviceId,
            int priorityNumber)
        {
            AddModelDto model = new(
                ModelId: modelId,
                ModelName: modelName,
                ModelSystemPrompt: modelSystemPrompt,
                ModelDescription: modelDescription,
                ModelInstructions: modelInstructions,
                ModelPurpose: modelPurpose,
                Temperature: temperature,
                ApiKey: apiKey,
                Endpoint: endpoint,
                ServiceId: serviceId,
                PriorityNumber: priorityNumber);

            return DispatchAddModelAsync(model);
        }

        [KernelFunction]
        [Description("Kayıtlı bir AI modelinin belirtilen alanını günceller")]
        public Task<string> UpdateModel(
            string modelName,
            string argumentName,
            string argumentValue)
        {
            return DispatchCommandAsync(
                $"/modelguncelle {modelName} {argumentName} {argumentValue}");
        }

        [KernelFunction]
        [Description("Model adına göre kayıtlı AI modelini siler")]
        public Task<string> RemoveModel(string modelName)
        {
            return DispatchCommandAsync($"/modelsil {modelName}");
        }

        [KernelFunction]
        [Description("DefneAI komutunu CommandDispatcher üzerinden çalıştırır")]
        public Task<string> ExecuteCommand(string command)
        {
            return DispatchCommandAsync(command);
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

        private static string FormatYouTubeDuration(
            TimeSpan? duration)
        {
            if (duration is null)
            {
                return "Bilinmiyor";
            }

            return duration.Value.TotalHours >= 1
                ? duration.Value.ToString(@"h\:mm\:ss")
                : duration.Value.ToString(@"m\:ss");
        }

        private static string ToSingleLine(string value)
        {
            return value.ReplaceLineEndings(" ").Trim();
        }

        private static string OpenUrlInDefaultBrowser(
            string url,
            string targetDescription)
        {
            try
            {
                using Process? process = Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });

                return $"{targetDescription} basariyla acildi.";
            }
            catch (Exception ex)
            {
                return $"{targetDescription} acilamadi: {ex.Message}";
            }
        }

        private async Task<string> DispatchAddModelAsync(
            AddModelDto model,
            CancellationToken cancellationToken = default)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            ICommandDispatcher commandDispatcher =
                scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

            return await commandDispatcher.AddModelAsync(
                model,
                cancellationToken);
        }

        private async Task<string> DispatchCommandAsync(
            string command,
            CancellationToken cancellationToken = default)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            ICommandDispatcher commandDispatcher =
                scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

            return await commandDispatcher.ExecuteAsync(command, cancellationToken);
        }
    }
}
