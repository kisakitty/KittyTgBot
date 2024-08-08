namespace KittyBot.database;

public class UserService
{
    private readonly KittyBotContext _db;

    public UserService(KittyBotContext db)
    {
        _db = db;
    }

    public bool IsAdmin(long telegramId)
    {
        return (from user in _db.Users where user.IsAdmin && user.UserId == telegramId select user)
            .FirstOrDefault() is not null;
    }

    public void InitAdmins(IEnumerable<long> adminsIds)
    {
        var currentAdminsIds = (from user in _db.Users where user.IsAdmin select user.UserId).ToHashSet();
        var updatedAdminIds = adminsIds.ToHashSet();
        var added = updatedAdminIds.Except(currentAdminsIds);
        foreach (long tgId in added)
        {
            CreateOrUpdateAdmin(_db, tgId);
        }

        _db.SaveChanges();
    }

    public void CreateOrUpdateUser(long telegramId, string? username, string firstName, string? lastName)
    {
        var updatedUser = (from user in _db.Users where user.UserId == telegramId select user).FirstOrDefault();
        if (updatedUser is null)
        {
            _db.Users.Add(new User
                { UserId = telegramId, Username = username, FirstName = firstName, LastName = lastName });
        }
        else
        {
            updatedUser.Username = username;
            updatedUser.FirstName = firstName;
            updatedUser.LastName = lastName;
        }

        _db.SaveChanges();
    }

    private static void CreateOrUpdateAdmin(KittyBotContext db, long tgId)
    {
        var newAdmin = (from user in db.Users where user.UserId == tgId select user).FirstOrDefault();
        if (newAdmin is not null)
        {
            newAdmin.IsAdmin = true;
        }
        else
        {
            db.Users.Add(new User { UserId = tgId, IsAdmin = true, FirstName = "" });
        }
    }
}