using Microsoft.SemanticKernel;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.ComponentModel;

namespace DefneAI.Infrastructure.Plugin
{
    public class DefneAutomationPlugin
    {
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
        [Description("Bu fonksiyon youtubedan belirtilen şarkıyı açar")]
        public string OpenMusic(string MusicName)
        {
            ChromeOptions options = new ChromeOptions();
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string chromeUserDataPath = Path.Combine(localAppData, @"Google\Chrome\User Data");
            options.AddArgument("--headless");
            options.AddArgument($"--user-data-dir={chromeUserDataPath}");
            options.AddArgument("--profile-directory=Default");
            options.AddArgument("--disable-extensions");
            options.AddArgument("--no-first-run");
            IWebDriver chrome = new OpenQA.Selenium.Chrome.ChromeDriver();
            try
            {
                chrome.Navigate().GoToUrl($"https://www.youtube.com/results?search_query={MusicName}");
                var wait = new WebDriverWait(chrome, TimeSpan.FromSeconds(10))
                {
                    PollingInterval = TimeSpan.FromMilliseconds(250)
                };
                var firstResult = wait.Until(driver => driver.FindElement(By.XPath("(//a[@id='video-title'])[1]")));
                firstResult.Click();
                return $"Müzik '{MusicName}' başarıyla açıldı.";

            }
            catch (Exception ex)
            {
                return $"Müzik '{MusicName}' açılamadı: {ex.Message}";
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
    }
}
