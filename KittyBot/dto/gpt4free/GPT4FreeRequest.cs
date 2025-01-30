namespace KittyBot.dto.gpt4free;

public record GPT4FreeRequest(string model, string? provider, bool stream, List<GPT4FreeMessage> messages);