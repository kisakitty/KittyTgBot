namespace KittyBot.dto.gemini;

public record GeminiCandidate(
    GeminiMessage? content,
    string? finishReason,
    int? index,
    List<GeminiSafetyRating>? safetyRatings);