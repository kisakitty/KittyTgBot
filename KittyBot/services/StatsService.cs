using KittyBot.database;
using Microsoft.EntityFrameworkCore;

namespace KittyBot.services;

public class StatsService
{
    private readonly KittyBotContext _db;

    public StatsService(KittyBotContext db)
    {
        _db = db;
    }

    public void LogStats(Telegram.Bot.Types.User tgUser, long chatId, long messageId)
    {
        CountMessage(tgUser, chatId);
        RememberAuthor(tgUser, chatId, messageId);
    }

    public User? GetMessageAuthor(long chatId, long messageId)
    {
        return (from m in _db.CachedMessages
            where m.ChatId == chatId && m.MessageId == messageId
            select m.Author).FirstOrDefault();
    }

    private void RememberAuthor(Telegram.Bot.Types.User tgUser, long chatId, long messageId)
    {
        var user = GetOrCreateDbUser(tgUser);
        _db.CachedMessages.Add(new ChatMessage { Author = user, ChatId = chatId, MessageId = messageId});
        _db.SaveChanges();
    }

    private void CountMessage(Telegram.Bot.Types.User tgUser, long chatId)
    {
        var stats = (from s in _db.Stats 
                where s.User.UserId == tgUser.Id && s.ChatId == chatId
                select s)
            .FirstOrDefault();

        if (stats == null)
        {
            var user = GetOrCreateDbUser(tgUser);

            _db.Stats.Add(new Stats { ChatId = chatId, User = user, CountMessages = 1, IsActive = true });
        }
        else
        {
            stats.CountMessages += 1;
        }

        _db.SaveChanges();
    }

    public void ActivateUser(Telegram.Bot.Types.User tgUser, long chatId)
    {
        SetUserStatus(tgUser, chatId, true);
    }

    public void DeactivateUser(Telegram.Bot.Types.User tgUser, long chatId)
    {
        SetUserStatus(tgUser, chatId, false);
    }

    private void SetUserStatus(Telegram.Bot.Types.User tgUser, long chatId, bool isActive)
    {
        var stats = (from s in _db.Stats 
                where s.User.UserId == tgUser.Id && s.ChatId == chatId
                select s)
            .FirstOrDefault();
        if (stats == null)
        {
            var user = GetOrCreateDbUser(tgUser);
            _db.Stats.Add(new Stats { ChatId = chatId, User = user, CountMessages = 0, IsActive = isActive });
        }
        else
        {
            stats.IsActive = isActive;
        }

        _db.SaveChanges();
    }

    // TODO duplication
    private User GetOrCreateDbUser(Telegram.Bot.Types.User tgUser)
    {
        var user = (from u in _db.Users where u.UserId == tgUser.Id select u).FirstOrDefault();
        if (user is null)
        {
            user = _db.Users.Add(new User
                { UserId = tgUser.Id, Username = tgUser.Username, FirstName = tgUser.FirstName, LastName = tgUser.LastName }).Entity;
        }

        return user;
    }

    public List<KeyValuePair<string, long>> GetGlobalStatsLinks(long chatId, bool mention)
    {
        var stats = from s in _db.Stats
                orderby s.CountMessages descending 
                where s.ChatId == chatId
                select s;
        return stats
            .Include(s => s.User)
            .Select(s => KeyValuePair.Create(Util.FormatUserName(s.User, mention), s.CountMessages))
            .ToList();
    }

    public List<long> GetUserChats(long userId)
    {
        var chats = from s in _db.Stats where s.User.Id == userId && s.ChatId < 0 && s.IsActive select s.ChatId;
        return chats.ToList();
    }
}