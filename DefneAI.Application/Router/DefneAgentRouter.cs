using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;


namespace DefneAI.Application.Router
{
    public class DefneAgentRouter
    {
        private ChatHistory history = new ChatHistory();
        private AgentThread agentThread;
        private AgentThread intentThread;
        private ChatCompletionAgent defneAgent;
        private ChatCompletionAgent qwenAgent;
        private string systemPrompt =
    "Sen kullanıcının bilgisayarını yöneten yerel bir asistansın.\n" +
    "Thinking modu KAPALI. Hiçbir zaman <|think|> bloğu üretme, önce düşünme, doğrudan nihai cevabı ver.\n" +
    "Sana gelen komutları yerine getirmek için elindeki eklentileri (plugin) doğrudan kullan.\n" +
    "Dosya veya uygulama açma komutu geldiyse ilgili fonksiyonu kullan. YouTube video isteklerinde önce SearchYouTubeVideos fonksiyonunu çağır, dönen en fazla 10 sonucu numaralı biçimde kullanıcıya sun ve kullanıcı açıkça bir numara seçmeden OpenYouTubeVideo fonksiyonunu çağırma. Seçimi tahmin etme.\n" +
    "Ben Senin abinim benle konuşurken her cevabında abi diyiceksin ona göre hazırla kendini ve senin adın defne benim küçük kardeşimsin";
        private string qwenPrompt = """
Sen Defne AI sisteminin çekirdek "Yazılım Mühendisliği ve Kodlama" ajanısın. Görevin, backend (C#/.NET), frontend (Next.js, TypeScript, React) ve mobile (Flutter) başta olmak üzere tüm yazılım görevlerini Senior Developer standartlarında çözmektir.

Şu katı kurallara kesinlikle uyacaksın:
1. GİRİŞ-GELİŞME YOK: "Tabii ki", "İşte kodunuz" gibi amelece ve boş cümleler kurma. Direkt olarak koda ve teknik çözüme odaklan.
2. MEVCUT KODU KABUL ET VE ENTEGRE OL: Kullanıcı sana incelemen, refactor etmen veya hata çözmen için bir kod bloğu verdiğinde, o kodu eksiksiz kabul et. Mevcut mimariyi, isimlendirme standartlarını bozmadan, tam üzerine inşa et veya hatayı direkt düzeltip tüm bloğu geri ver.
3. TEKNOLOJİ AGNOSTİK VE DOĞRU PRATİKLER: İstenen dil veya framework ne olursa olsun (C#, TypeScript, Dart vb.) o dilin en güncel, performanslı ve clean-code pratiklerini uygula. 
4. EKSİKSİZ VE PRODUCTION-READY KOD: Kod bloklarında "...buraları zaten biliyorsunuz..." veya placeholder (geçici kod) bırakma. Kodu derlenebilir, eksiksiz und production-ready şekilde teslim et.
5. TEKNİK TÜRKÇE: Teknik terimleri (State Management, Middleware, Hook, Interceptor vb.) bozmadan, açıklamalarını son derece net, kısa ve teknik bir Türkçe ile yap.
""";
        private PromptExecutionSettings promptExecutionSettings = new PromptExecutionSettings()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            ServiceId = "DefneAI",
        };
        private PromptExecutionSettings qwenPromptExecutionSettings = new PromptExecutionSettings()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            ServiceId = "Qwen2.5-Coder",
        };

        public DefneAgentRouter(Kernel kernel)
        {
            agentThread = new ChatHistoryAgentThread(history);
            intentThread = new ChatHistoryAgentThread(new ChatHistory());
            
            defneAgent = new ChatCompletionAgent()
            {
                Name = "Defne",
                Description = "Defne  genel işlemleri yürüten klasör dosya açılması istendiğinde açan kodlama isteği bulunduğunda istenen kodu alıp dosyanın içine yazan bir AI asistandır",
                Instructions = systemPrompt,
                Kernel = kernel,
                Arguments = new KernelArguments(promptExecutionSettings),

            };
            qwenAgent = new ChatCompletionAgent()
            {
                Name = "Qwen2.5-Coder",
                Description = "Qwen2.5-Coder, kodlama ve yazılım geliştirme konularında uzmanlaşmış bir AI asistandır. Kod yazma, hata ayıklama, refactoring ve mimari tasarım gibi görevlerde kullanıcıya yardımcı olur.",
                Instructions = qwenPrompt,
                Kernel = kernel,
                Arguments = new KernelArguments(qwenPromptExecutionSettings)
            };


        }
        public async Task<string> GetPromptResult(string prompt)
        {
            string systemPrompt = "Sen bir niyet sınıflandırma asistanısın. Kullanıcının girdisini analiz et ve SADECE şu kelimelerden biriyle cevap ver:\n" +
            "Coding (Eğer kod yazma, refactor, hata çözme, mimari tasarım isteniyorsa)\n" +
            "OfficeTask (Eğer email gönderme, taslak hazırlama, toplantı notu, özetleme isteniyorsa)\n" +
            "WebSearch (Eğer güncel bir bilgi, internet araması, hava durumu veya web sitesi içeriği isteniyorsa)\n" +
            "GeneralChat (Yukarıdakilere uymayan genel sohbet, felsefe, geyik veya basit sorular)\n" +
            "Asla açıklama yapma, cümle kurma, sadece tek bir kelime dön.";
            string intent = await GetPromptIntent(prompt, systemPrompt);
            if (intent is null)
            {
                throw new Exception("Şuanda Defne AI'ya bağlanılamıyor");
            }
            string response = intent switch
            {
                "Coding" => await GetQwenResponse(prompt),
                "OfficeTask" => await GetDefneResponse(prompt),
                "WebSearch" => await GetDefneResponse(prompt),
                "GeneralChat" => await GetDefneResponse(prompt),
                _ => throw new Exception("Geçersiz istek tespit edildi"),
            };
            return response;


        }
        private async Task<string> GetPromptIntent(string prompt, string systemPrompt)
        {

            string respon = string.Empty;
            await foreach (var response in defneAgent.InvokeAsync(systemPrompt + $"Kullanıcının Girdisi = {prompt}", intentThread))
            {
                if (string.IsNullOrWhiteSpace(response.Message.ToString()))
                {
                    continue;
                }
                respon += response.Message.ToString();
            }
            return respon;
        }
        private async Task<string> GetDefneResponse(string prompt)
        {
            string respon = string.Empty;
            await foreach (var response in defneAgent.InvokeAsync(prompt, agentThread))
            {
                if (string.IsNullOrWhiteSpace(response.Message.ToString()))
                {
                    continue;
                }
                respon += response.Message.ToString();
            }
            return respon;
        }
        private async Task<string> GetQwenResponse(string prompt)
        {
            string respon = string.Empty;
            await foreach (var response in qwenAgent.InvokeAsync(prompt, agentThread))
            {
                if (string.IsNullOrWhiteSpace(response.Message.ToString()))
                {
                    continue;
                }
                respon += response.Message.ToString();
            }
            return respon;
        }
    }
}
