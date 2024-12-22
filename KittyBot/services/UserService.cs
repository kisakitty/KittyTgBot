using KittyBot.database;

namespace KittyBot.services;

public class UserService(KittyBotContext db) : BaseService(db)
{
    public bool IsAdmin(long telegramId)
    {
        return (from user in Db.Users where user.IsAdmin && user.UserId == telegramId select user)
            .FirstOrDefault() is not null;
    }

    public void InitAdmins(IEnumerable<long> adminsIds)
    {
        var currentAdminsIds = (from user in Db.Users where user.IsAdmin select user.UserId).ToHashSet();
        var updatedAdminIds = adminsIds.ToHashSet();
        var added = updatedAdminIds.Except(currentAdminsIds);
        foreach (var tgId in added) CreateOrUpdateAdmin(Db, tgId);

        Db.SaveChanges();
    }

    public void CreateOrUpdateUser(long telegramId, string? username, string firstName, string? lastName)
    {
        var updatedUser = (from user in Db.Users where user.UserId == telegramId select user).FirstOrDefault();
        if (updatedUser is null)
        {
            Db.Users.Add(new User
                { UserId = telegramId, Username = username, FirstName = firstName, LastName = lastName });
        }
        else
        {
            updatedUser.Username = username;
            updatedUser.FirstName = firstName;
            updatedUser.LastName = lastName;
        }

        Db.SaveChanges();
    }

    private static void CreateOrUpdateAdmin(KittyBotContext db, long tgId)
    {
        var newAdmin = (from user in db.Users where user.UserId == tgId select user).FirstOrDefault();
        if (newAdmin is not null)
            newAdmin.IsAdmin = true;
        else
            db.Users.Add(new User { UserId = tgId, IsAdmin = true, FirstName = "" });
    }
}