using Microsoft.SemanticKernel;
namespace DefneAI.Application.Router
{
    public class DefneAgentRouter
    {


        public DefneAgentRouter(Kernel kernel)
        {

        }
        public async Task<string> GetPromptResult(string prompt)
        {
            string systemPrompt = "Sen bir niyet sınıflandırma asistanısın. Kullanıcının girdisini analiz et ve SADECE şu kelimelerden biriyle cevap ver:\n" +
            "Coding (Eğer kod yazma, refactor, hata çözme, mimari tasarım isteniyorsa)\n" +
            "OfficeTask (Eğer email gönderme, taslak hazırlama, toplantı notu, özetleme isteniyorsa)\n" +
            "WebSearch (Eğer güncel bir bilgi, internet araması, hava durumu veya web sitesi içeriği isteniyorsa)\n" +
            "GeneralChat (Yukarıdakilere uymayan genel sohbet, felsefe, geyik veya basit sorular)\n" +
            "Asla açıklama yapma, cümle kurma, sadece tek bir kelime dön.";
            return default;


        }


    }
}
