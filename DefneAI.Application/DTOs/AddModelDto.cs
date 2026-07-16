namespace DefneAI.Application.DTOs;

public sealed record AddModelDto(
    string ModelId,
    string ModelName,
    string ModelSystemPrompt,
    string ModelDescription,
    string ModelInstructions,
    double Temperature,
    string ApiKey,
    string Endpoint,
    string ServiceId,
    int PriorityNumber);
