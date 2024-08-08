namespace KittyBot.dto;

public record GPT4FreeRequest(string model, string? provider, bool stream, List<GPT4FreeMessage> messages);