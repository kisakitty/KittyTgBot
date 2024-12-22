using KittyBot.database;

namespace KittyBot.services;

public class LocaleService(KittyBotContext db) : BaseService(db)
{
    public Locale GetLocale(long chatId)
    {
        var locale =
            (from chatLocale in Db.ChatsLanguages
                where chatLocale.ChatId == chatId
                select chatLocale).FirstOrDefault()?.Language;

        return locale ?? Locale.EN;
    }

    public void SetLanguage(long chatId, Locale language)
    {
        Db.ChatsLanguages
            .Where(chat => chat.ChatId == chatId)
            .ToList()
            .ForEach(chat => chat.Language = language);
        Db.SaveChanges();
    }
}