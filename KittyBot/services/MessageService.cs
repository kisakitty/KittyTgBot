using KittyBot.database;
using KittyBot.dto;
using KittyBot.dto.gemini;
using OpenAI;
using OpenAI.Chat;

namespace KittyBot.services;

public class MessageService
{
    private readonly KittyBotContext _db;

    public MessageService(KittyBotContext db)
    {
        _db = db;
    }
    
    public List<Message> GetPreviousMessages(long chatId, int limit)
    {
        var messages = 
                from msg in _db.Messages 
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
                from msg in _db.Messages 
                orderby msg.Id
                where msg.ChatId == chatId 
                select msg;
        return messages
            .Skip(Math.Max(0, messages.Count() - limit))
            .Select(message => new GPT4FreeMessage(message.IsBot ? "assistant" : "user", message.Content))
            .ToList();
    }
    
    public List<GeminiMessage> GetPreviousMessagesGemini(long chatId, int limit)
    {
        var messages = 
                from msg in _db.Messages 
                orderby msg.Id
                where msg.ChatId == chatId 
                select msg;
        return messages
            .Skip(Math.Max(0, messages.Count() - limit))
            .Select(message => new GeminiMessage(new List<GeminiContent> { new(message.Content, null) }, message.IsBot ? "model" : "user"))
            .ToList();
    }

    public void LogMessage(HistoricalMessage newMessage)
    {
        _db.Messages.Add(newMessage);
        _db.SaveChanges();
    }
}