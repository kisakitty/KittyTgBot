namespace KittyBot.dto;

public record GPT4FreeUsage(int prompt_tokens, int completion_tokens, int total_tokens);