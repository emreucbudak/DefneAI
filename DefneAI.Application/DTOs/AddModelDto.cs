namespace DefneAI.Application.DTOs;

using DefneAI.Domain.Enums;

public sealed record AddModelDto(
    string ModelId,
    string ModelName,
    string ModelSystemPrompt,
    string ModelDescription,
    string ModelInstructions,
    AITaskType ModelPurpose,
    double Temperature,
    string ApiKey,
    string Endpoint,
    string ServiceId,
    int PriorityNumber);
