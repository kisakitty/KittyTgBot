using KittyBot.database;

namespace KittyBot.services;

public class LocaleService
{
    private readonly KittyBotContext _db;

    public LocaleService(KittyBotContext db)
    {
        _db = db;
    }
    
    public Locale GetLocale(long chatId)
    {
        var locale =
            (from chatLocale in _db.ChatsLanguages 
                where chatLocale.ChatId == chatId
                select chatLocale).FirstOrDefault()?.Language;
        
        return locale ?? Locale.EN;
    }

    public void SetLanguage(long chatId, Locale language)
    {
        _db.ChatsLanguages
            .Where(chat => chat.ChatId  == chatId)
            .ToList()
            .ForEach(chat => chat.Language = language);
        _db.SaveChanges();
    }
}