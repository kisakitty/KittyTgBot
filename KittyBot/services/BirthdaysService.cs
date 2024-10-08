using KittyBot.database;
using KittyBot.exceptions;
using Microsoft.EntityFrameworkCore;

namespace KittyBot.services;

public class BirthdaysService
{
    private readonly KittyBotContext _db;

    public BirthdaysService(KittyBotContext db)
    {
        _db = db;
    }

    public List<Birthday> GetTodayBirthdays()
    {
        var now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
        var todayBirthdays = (from birthday in _db.Birthdays
            where birthday.Day == now.Day && birthday.Month == now.Month
            select birthday)
            .Include(birthday => birthday.User)
            .ToList();
        return todayBirthdays;
    }

    public List<Birthday> GetBirthdaysThisMonth(long chatId)
    {
        var requestedChats = chatId < 0
            ? [chatId] // chatId is a group id
            : (from s in _db.Stats where s.User.UserId == chatId && s.IsActive select s.ChatId).ToList(); // chatId is a user id
        var usersWhiteList = from s in _db.Stats where requestedChats.Contains(s.ChatId) && s.IsActive select s.User;
        var now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
        var todayBirthdays = (from birthday in _db.Birthdays
            where birthday.Month == now.Month & (usersWhiteList == null || usersWhiteList.Contains(birthday.User))
            orderby birthday.Day
            select birthday)
            .Include(birthday => birthday.User)
            .ToList();
        return todayBirthdays;
    }
    
    public void SetBirthday(string username, int day, int month)
    {
        if (!ValidateDate(day, month))
        {
            throw new InvalidBirthdayException();
        }
        var birthday = (from b in _db.Birthdays where b.User.Username == username select b).FirstOrDefault();
        if (birthday == null)
        {
            var user = (from u in _db.Users where u.Username == username select u).FirstOrDefault();
            if (user == null)
            {
                throw new NoFoundException();
            }
            _db.Birthdays.Add(new Birthday { Day = day, Month = month, User = user });
        }
        else
        {
            birthday.Day = day;
            birthday.Month = month;
        }

        _db.SaveChanges();
    }

    public void SetBirthday(Telegram.Bot.Types.User tgUser, int day, int month)
    {
        if (!ValidateDate(day, month))
        {
            throw new InvalidBirthdayException();
        }
        var birthday = (from b in _db.Birthdays where b.User.UserId == tgUser.Id select b).FirstOrDefault();
        if (birthday == null)
        {
            var user = GetOrCreateDbUser(tgUser);
            _db.Birthdays.Add(new Birthday { Day = day, Month = month, User = user });
        }
        else
        {
            birthday.Day = day;
            birthday.Month = month;
        }

        _db.SaveChanges();
    }

    public bool RemoveBirthday(Telegram.Bot.Types.User tgUser)
    {
        var birthday = (from b in _db.Birthdays where b.User.UserId == tgUser.Id select b).FirstOrDefault();
        if (birthday == null)
        {
            return false;
        }

        _db.Remove(birthday);
        _db.SaveChanges();
        return true;
    }

    private User GetOrCreateDbUser(Telegram.Bot.Types.User tgUser)
    {
        var user = (from u in _db.Users where u.UserId == tgUser.Id select u).FirstOrDefault();
        if (user is null)
        {
            user = _db.Users.Add(new User
            {
                UserId = tgUser.Id, Username = tgUser.Username, FirstName = tgUser.FirstName, LastName = tgUser.LastName
            }).Entity;
        }

        return user;
    }

    private bool ValidateDate(int day, int month)
    {
        if (month is < 1 or > 12)
        {
            return false;
        }
        if (month == 2)
        {
            return day <= 29;
        }
        return day > 0 && day <= DateTime.DaysInMonth(2024, month);
    }
}