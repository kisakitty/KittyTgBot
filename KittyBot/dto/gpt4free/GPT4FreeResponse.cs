namespace KittyBot.dto.gpt4free;

public record GPT4FreeResponse(
    string id,
    long created,
    string? model,
    string? provider,
    List<GPT4FreeChoice> choices,
    GPT4FreeUsage usage);