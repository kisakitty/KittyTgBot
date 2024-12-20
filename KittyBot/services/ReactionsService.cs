using KittyBot.database;
using KittyBot.dto;

namespace KittyBot.services;

public class ReactionsService
{
    private readonly KittyBotContext _db;

    public ReactionsService(KittyBotContext db)
    {
        _db = db;
    }

    public void LogReaction(User receiver, long chatId, string emoji)
    {
        var currentReactionCount = (from reaction in _db.Reactions
                where reaction.ChatId == chatId && receiver.Equals(reaction.User) && emoji.Equals(reaction.Emoji)
                select reaction)
            .FirstOrDefault();
        if (currentReactionCount == null)
        {
            _db.Reactions.Add(new Reaction { ChatId = chatId, Count = 1, Emoji = emoji, User = receiver });
        }
        else
        {
            currentReactionCount.Count += 1;
        }

        _db.SaveChanges();
    }


    public void RemoveReaction(User receiver, long chatId, string emoji)
    {
        var currentReactionCount = (from reaction in _db.Reactions
                where reaction.ChatId == chatId && receiver.Equals(reaction.User) && emoji.Equals(reaction.Emoji)
                select reaction)
            .FirstOrDefault();
        if (currentReactionCount == null) return;
        if (currentReactionCount.Count == 1)
            _db.Reactions.Remove(currentReactionCount);
        else
            currentReactionCount.Count -= 1;
        _db.SaveChanges();
    }

    public List<ReactionStatByUser> GetChatStatistics(long chatId)
    {
        return _db.Reactions
            .Where(r => r.ChatId == chatId)
            .GroupBy(r => r.User, r => r)
            .Select(g => new ReactionStatByUser(
                Util.FormatUserName(g.Key, false),
                g.Sum(r => r.Count),
                 g
                     .GroupBy(r => r.Emoji)
                     .OrderByDescending(eg => eg.Sum(r => r.Count))
                     .First()
                     .Key
            )).ToList();
    }

    public List<ReactionStatByGroups> GetUserStatistics(long userTgId)
    {
        return _db.Reactions
            .Where(r => r.User.UserId == userTgId)
            .GroupBy(r => r.ChatId, r => r)
            .Select(g => new ReactionStatByGroups(
                g.Key,
                g.Sum(r => r.Count),
                 g
                     .GroupBy(r => r.Emoji)
                     .OrderByDescending(eg => eg.Sum(r => r.Count))
                     .First()
                     .Key
            )).ToList();
    }

    // TODO duplication
    private database.User GetOrCreateDbUser(Telegram.Bot.Types.User tgUser)
    {
        var user = (from u in _db.Users where u.UserId == tgUser.Id select u).FirstOrDefault();
        if (user is null)
            user = _db.Users.Add(new database.User
            {
                UserId = tgUser.Id, Username = tgUser.Username, FirstName = tgUser.FirstName, LastName = tgUser.LastName
            }).Entity;

        return user;
    }
}