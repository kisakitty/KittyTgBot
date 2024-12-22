namespace KittyBot.dto.gemini;

public record GeminiRequest(
    List<GeminiMessage> contents,
    GeminiSystemInstruction? system_instruction,
    GeminiConfig? generationConfig,
    List<GeminiSafetyParameter>? safetySettings);