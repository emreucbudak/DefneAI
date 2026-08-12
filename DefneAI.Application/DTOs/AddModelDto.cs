namespace DefneAI.Application.DTOs;

public sealed record AddModelDto(
    string ModelName,
    string Provider,
    string ApiKey,
    string ModelPurpose,
    string ModelDescription,
    double Temperature,
    int PriorityNumber);
