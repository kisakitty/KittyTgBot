namespace KittyBot.dto.gpt4free;

public record GPT4FreeChoice(long index, GPT4FreeMessage message, string finish_reason);