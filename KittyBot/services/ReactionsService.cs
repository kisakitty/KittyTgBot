using KittyBot.database;
using User = Telegram.Bot.Types.User;

namespace KittyBot.services;

public class ReactionsService
{
    private readonly KittyBotContext _db;

    public ReactionsService(KittyBotContext db)
    {
        _db = db;
    }

    public void LogReaction(User tgUser, long chatId, string emoji)
    {
        var currentReactionCount = (from reaction in _db.Reactions
                where reaction.ChatId == chatId && reaction.User.UserId == tgUser.Id && reaction.Emoji == emoji
                select reaction)
            .FirstOrDefault();
        if (currentReactionCount == null)
        {
            var user = GetOrCreateDbUser(tgUser);
            _db.Reactions.Add(new Reaction { ChatId = chatId, Count = 1, Emoji = emoji, User = user });
        }
        else
        {
            currentReactionCount.Count += 1;
        }

        _db.SaveChanges();
    }


    public void RemoveReaction(User tgUser, long chatId, string emoji)
    {
        var currentReactionCount = (from reaction in _db.Reactions
                where reaction.ChatId == chatId && reaction.User.UserId == tgUser.Id && reaction.Emoji == emoji
                select reaction)
            .FirstOrDefault();
        if (currentReactionCount == null) return;
        if (currentReactionCount.Count == 1)
            _db.Reactions.Remove(currentReactionCount);
        else
            currentReactionCount.Count -= 1;
        _db.SaveChanges();
    }

    // TODO duplication
    private database.User GetOrCreateDbUser(User tgUser)
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