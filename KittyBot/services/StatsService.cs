using KittyBot.database;
using KittyBot.utility;
using Microsoft.EntityFrameworkCore;
using User = Telegram.Bot.Types.User;

namespace KittyBot.services;

public class StatsService(KittyBotContext db) : BaseService(db)
{
    public void LogMessage(User tgUser, long chatId, long messageId)
    {
        CountMessage(tgUser, chatId);
        RememberAuthor(tgUser, chatId, messageId);
    }

    public database.User? GetMessageAuthor(long chatId, long messageId)
    {
        return (from m in Db.CachedMessages
            where m.ChatId == chatId && m.MessageId == messageId
            select m.Author).FirstOrDefault();
    }

    private void RememberAuthor(User tgUser, long chatId, long messageId)
    {
        var user = GetOrCreateDbUser(tgUser);
        Db.CachedMessages.Add(new ChatMessage { Author = user, ChatId = chatId, MessageId = messageId });
        Db.SaveChanges();
    }

    private void CountMessage(User tgUser, long chatId)
    {
        var stats = (from s in Db.Stats
                where s.User.UserId == tgUser.Id && s.ChatId == chatId
                select s)
            .FirstOrDefault();

        if (stats == null)
        {
            var user = GetOrCreateDbUser(tgUser);

            Db.Stats.Add(new Stats { ChatId = chatId, User = user, CountMessages = 1, IsActive = true });
        }
        else
        {
            stats.CountMessages += 1;
            stats.IsActive = true;
        }

        Db.SaveChanges();
    }

    public void ActivateUser(User tgUser, long chatId)
    {
        SetUserStatus(tgUser, chatId, true);
    }

    public void DeactivateUser(User tgUser, long chatId)
    {
        SetUserStatus(tgUser, chatId, false);
    }

    private void SetUserStatus(User tgUser, long chatId, bool isActive)
    {
        var stats = (from s in Db.Stats
                where s.User.UserId == tgUser.Id && s.ChatId == chatId
                select s)
            .FirstOrDefault();
        if (stats == null)
        {
            var user = GetOrCreateDbUser(tgUser);
            Db.Stats.Add(new Stats { ChatId = chatId, User = user, CountMessages = 0, IsActive = isActive });
        }
        else
        {
            stats.IsActive = isActive;
        }

        Db.SaveChanges();
    }

    public List<KeyValuePair<string, long>> GetGlobalStatsLinks(long chatId, bool mention, bool userIdLink)
    {
        var stats = from s in Db.Stats
            orderby s.CountMessages descending
            where s.ChatId == chatId && s.CountMessages > 0
            select s;
        return stats
            .Include(s => s.User)
            .Select(s => KeyValuePair.Create(Util.FormatUserName(s.User, mention, userIdLink), s.CountMessages))
            .ToList();
    }

    public void DeactivateChat(long chatId)
    {
        var stats = from s in Db.Stats
            where s.ChatId == chatId
            select s;
        foreach (var s in stats)
            if (s != null)
                s.IsActive = false;

        Db.SaveChanges();
    }

    public List<long> GetAllActiveChats()
    {
        return Db.Stats.Where(s => s.IsActive && s.ChatId < 0)
            .Select(s => s.ChatId)
            .Distinct()
            .ToList();
    }

    public List<long> GetUserChats(long userId)
    {
        var chats = from s in Db.Stats where s.User.Id == userId && s.ChatId < 0 && s.IsActive select s.ChatId;
        return chats.ToList();
    }
}