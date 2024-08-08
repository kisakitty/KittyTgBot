namespace KittyBot.dto.gemini;

public record GeminiError(int code, string message, string status);