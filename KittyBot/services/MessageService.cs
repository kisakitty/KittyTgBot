using KittyBot.database;
using KittyBot.dto;
using KittyBot.dto.gemini;
using KittyBot.dto.gpt4free;
using OpenAI;
using OpenAI.Chat;

namespace KittyBot.services;

public class MessageService(KittyBotContext db) : BaseService(db)
{
    public List<Message> GetPreviousMessages(long chatId, int limit)
    {
        var messages =
            from msg in Db.Messages
            orderby msg.Id
            where msg.ChatId == chatId
            select msg;
        return messages
            .Skip(Math.Max(0, messages.Count() - limit))
            .Select(message => new Message(message.IsBot ? Role.Assistant : Role.User, message.Content, null))
            .ToList();
    }

    public List<GPT4FreeMessage> GetPreviousMessagesGpt4Free(long chatId, int limit)
    {
        var messages =
            from msg in Db.Messages
            orderby msg.Id
            where msg.ChatId == chatId
            select msg;
        return messages
            .Skip(Math.Max(0, messages.Count() - limit))
            .Select(message => new GPT4FreeMessage(message.IsBot ? "assistant" : "user", message.Content))
            .ToList();
    }

    public List<GeminiMessage> GetPreviousMessagesGemini(long chatId, int limit, ChatMode mode)
    {
        var messages =
            from msg in Db.Messages
            orderby msg.Id
            where msg.ChatId == chatId && msg.ChatMode == mode
            select msg;
        return messages
            .Skip(Math.Max(0, messages.Count() - limit))
            .Select(message => new GeminiMessage(new List<GeminiContent> { new(message.Content, null) },
                message.IsBot ? "model" : "user"))
            .ToList();
    }

    public void ClearChatMessages(long chatId, ChatMode mode)
    {
        var chatMessages = Db.Messages.Where(m => m.ChatId == chatId && m.ChatMode == mode).ToList();
        Db.Messages.RemoveRange(chatMessages);
        Db.SaveChanges();
    }

    public void LogMessage(HistoricalMessage newMessage)
    {
        Db.Messages.Add(newMessage);
        Db.SaveChanges();
    }
}