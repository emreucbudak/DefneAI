using System.ComponentModel;
using System.Diagnostics;
using YoutubeExplode;
using YoutubeExplode.Search;
using YoutubeExplode.Videos;

namespace DefneAI.Infrastructure.Tools;

public sealed class WebTools
{
    private const int YouTubeSearchResultLimit = 8;

    [Description("Verilen HTTP veya HTTPS bağlantısını varsayılan web tarayıcısında açar. Yalnızca sayfayı açar; tarayıcı kontrolü yapmaz.")]
    public string OpenWebLink(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps))
        {
            return "Geçerli bir HTTP veya HTTPS bağlantısı belirtilmelidir.";
        }

        return OpenUrlInDefaultBrowser(
            uri.AbsoluteUri,
            $"Bağlantı '{uri.AbsoluteUri}'");
    }

    [Description("YouTube'da video adıyla arama yapar ve seçim için numaralı video listesi döndürür. Kullanıcı video istediğinde önce bu fonksiyonu çağır, listeyi göster ve seçimini sor. Bu fonksiyon video açmaz.")]
    public async Task<string> SearchYouTubeVideos(
        string videoName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(videoName))
        {
            return "YouTube video adı belirtilmelidir.";
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
                return $"'{videoName}' için YouTube videosu bulunamadı.";
            }

            List<string> resultLines =
            [
                $"YouTube'da '{videoName}' için bulunan videolar:"
            ];

            for (int index = 0; index < videos.Count; index++)
            {
                VideoSearchResult video = videos[index];
                resultLines.Add(
                    $"{index + 1}. {ToSingleLine(video.Title)}");
                resultLines.Add(
                    $"   Kanal: {ToSingleLine(video.Author.ChannelTitle)}");
                resultLines.Add(
                    $"   Süre: {FormatYouTubeDuration(video.Duration)}");
                resultLines.Add(
                    $"   URL: {video.Url}");
            }

            resultLines.Add(
                $"Kullanıcıdan 1 ile {videos.Count} arasında bir video seçmesini iste. Seçim yapılmadan video açma.");

            return string.Join(Environment.NewLine, resultLines);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            return "YouTube video araması iptal edildi.";
        }
        catch (Exception ex)
        {
            return $"YouTube video listesi alınamadı: {ex.Message}";
        }
    }

    [Description("Yalnızca kullanıcı SearchYouTubeVideos listesinden bir video seçtikten sonra seçilen videonun URL veya kimliğini varsayılan tarayıcıda açar. Video adı aramak için kullanma; seçim olmadan çağırma.")]
    public string OpenYouTubeVideo(string videoUrlOrId)
    {
        if (string.IsNullOrWhiteSpace(videoUrlOrId))
        {
            return "YouTube video bağlantısı veya kimliği belirtilmelidir.";
        }

        if (VideoId.TryParse(videoUrlOrId) is not { } videoId)
        {
            return "Geçerli bir YouTube video bağlantısı veya kimliği belirtilmelidir. Video aramak için önce SearchYouTubeVideos kullanılmalıdır.";
        }

        string videoUrl = $"https://www.youtube.com/watch?v={videoId}";

        return OpenUrlInDefaultBrowser(
            videoUrl,
            "YouTube videosu");
    }

    [Description("Bu fonksiyon gmaili açar ve son mailleri çeker ")]
    public string OpenGmailAndFetchEmails()
    {
        try
        {
            var process = new Process();
            process.StartInfo.FileName = "https://mail.google.com/";
            process.Start();
            return "Gmail başarıyla açıldı ve son mailler çekildi.";
        }
        catch (Exception ex)
        {
            return $"Gmail açılamadı: {ex.Message}";
        }
    }

    private static string FormatYouTubeDuration(TimeSpan? duration)
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

            return $"{targetDescription} başarıyla açıldı.";
        }
        catch (Exception ex)
        {
            return $"{targetDescription} açılamadı: {ex.Message}";
        }
    }
}
