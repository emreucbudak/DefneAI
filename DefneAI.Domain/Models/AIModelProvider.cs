namespace DefneAI.Domain.Models
{
    using DefneAI.Domain.Enums;

    public class AIModelProvider
    {
        public int Id { get; set; }
        public string ModelId { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public string ModelSystemPrompt { get; set; } = string.Empty;
        public string ModelDescription { get; set; } = string.Empty;
        public string ModelInstructions { get; set; } = string.Empty;
        public AITaskType ModelPurpose { get; set; }
        public bool IsRemoved { get; set;}
        public double Temperature { get; set; }
        public string ApiKey { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string ServiceId { get; set; } = string.Empty;
        public int PriorityNumber { get; set; }
    }
}
