namespace DefneAI.Application.DTOs;

public sealed record AddModelDto(
    string ModelName,
    string Provider,
    string ApiKey);
