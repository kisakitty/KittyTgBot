using KittyBot.database;

namespace KittyBot.services;

public class ResponseConfigService
{
    private readonly KittyBotContext _db;

    public ResponseConfigService(KittyBotContext db)
    {
        _db = db;
    }

    public ResponseConfig GetResponseConfig(long chatId)
    {
        var config = GetChatConfig(chatId);

        if (config == null)
        {
            var defaultConfig = new ResponseConfig { ChatId = chatId, HelloMessage = true, ChatBot = true, Mode = ChatMode.NEUTRAL };
            _db.Add(defaultConfig);
            _db.SaveChanges();
            return defaultConfig;
        }

        return config;
    }

    public bool ReverseHelloMessageStatus(long chatId)
    {
        var config = GetChatConfig(chatId);
        
        if (config == null)
        {
            var newConfig = new ResponseConfig { ChatId = chatId, HelloMessage = false, ChatBot = true, Mode = ChatMode.NEUTRAL };
            _db.Add(newConfig);
            _db.SaveChanges();
            return false;
        }
        config.HelloMessage = !config.HelloMessage;
        _db.SaveChanges();
        return config.HelloMessage;
    }

    public bool ReverseChatBotStatus(long chatId)
    {
        var config = GetChatConfig(chatId);
        
        if (config == null)
        {
            var newConfig = new ResponseConfig { ChatId = chatId, HelloMessage = true, ChatBot = false, Mode = ChatMode.NEUTRAL };
            _db.Add(newConfig);
            _db.SaveChanges();
            return false;
        }
        config.ChatBot = !config.ChatBot;
        _db.SaveChanges();
        return config.ChatBot;
    }

    public ChatMode GetChatMode(long chatId)
    {
        var config = GetChatConfig(chatId);
        if (config != null) return config.Mode;
        var newConfig = new ResponseConfig { ChatId = chatId, HelloMessage = true, ChatBot = false, Mode = ChatMode.NEUTRAL };
        _db.Add(newConfig);
        _db.SaveChanges();
        return ChatMode.NEUTRAL;

    }

    public void SetChatMode(long chatId, ChatMode mode)
    {
        var config = GetChatConfig(chatId);
        if (config == null)
        {
            _db.Add(new ResponseConfig { ChatId = chatId, HelloMessage = true, ChatBot = false, Mode = mode });
        }
        else
        {
            config.Mode = mode;
        }
        _db.SaveChanges();
    }

    private ResponseConfig? GetChatConfig(long chatId)
    {
        var config = (from responseConfig in _db.ResponseConfigs
            where responseConfig.ChatId == chatId 
            select responseConfig).FirstOrDefault();
        return config;
    }
}