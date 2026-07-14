namespace DefneAI.Domain.Models
{
    public class AIModelProvider
    {
        public int Id { get; set; }
        public string ModelId { get; set; }
        public string ModelName { get; set; }
        public string ModelSystemPrompt { get; set; }
        public string ModelDescription { get; set; }
        public string ModelInstructions { get; set; }
        public bool IsRemoved { get; set;}
        public double Temperature { get; set; }
        public string ApiKey { get; set; }
        public string Endpoint { get; set; }
        public string ServiceId { get; set; }
        public int PriorityNumber { get; set; }
       
    }
}
