using KittyBot.database;

namespace KittyBot.services;

public class ResponseConfigService(KittyBotContext db) : BaseService(db)
{
    public ResponseConfig GetResponseConfig(long chatId)
    {
        var config = GetChatConfig(chatId);

        if (config != null) return config;
        var defaultConfig = new ResponseConfig
            { ChatId = chatId, HelloMessage = true, ChatBot = true, Mode = ChatMode.NEUTRAL };
        Db.Add(defaultConfig);
        Db.SaveChanges();
        return defaultConfig;

    }

    public bool ReverseHelloMessageStatus(long chatId)
    {
        var config = GetChatConfig(chatId);

        if (config == null)
        {
            var newConfig = new ResponseConfig
                { ChatId = chatId, HelloMessage = false, ChatBot = true, Mode = ChatMode.NEUTRAL };
            Db.Add(newConfig);
            Db.SaveChanges();
            return false;
        }

        config.HelloMessage = !config.HelloMessage;
        Db.SaveChanges();
        return config.HelloMessage;
    }

    public bool ReverseChatBotStatus(long chatId)
    {
        var config = GetChatConfig(chatId);

        if (config == null)
        {
            var newConfig = new ResponseConfig
                { ChatId = chatId, HelloMessage = true, ChatBot = false, Mode = ChatMode.NEUTRAL };
            Db.Add(newConfig);
            Db.SaveChanges();
            return false;
        }

        config.ChatBot = !config.ChatBot;
        Db.SaveChanges();
        return config.ChatBot;
    }

    public ChatMode GetChatMode(long chatId)
    {
        var config = GetChatConfig(chatId);
        if (config != null) return config.Mode;
        var newConfig = new ResponseConfig
            { ChatId = chatId, HelloMessage = true, ChatBot = false, Mode = ChatMode.NEUTRAL };
        Db.Add(newConfig);
        Db.SaveChanges();
        return ChatMode.NEUTRAL;
    }

    public void SetChatMode(long chatId, ChatMode mode)
    {
        var config = GetChatConfig(chatId);
        if (config == null)
            Db.Add(new ResponseConfig { ChatId = chatId, HelloMessage = true, ChatBot = false, Mode = mode });
        else
            config.Mode = mode;
        Db.SaveChanges();
    }

    private ResponseConfig? GetChatConfig(long chatId)
    {
        var config = (from responseConfig in Db.ResponseConfigs
            where responseConfig.ChatId == chatId
            select responseConfig).FirstOrDefault();
        return config;
    }
}