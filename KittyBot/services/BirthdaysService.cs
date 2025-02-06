using KittyBot.database;
using KittyBot.exceptions;
using Microsoft.EntityFrameworkCore;
using User = Telegram.Bot.Types.User;

namespace KittyBot.services;

public class BirthdaysService(KittyBotContext db) : BaseService(db)
{
    public List<Birthday> GetTodayBirthdays()
    {
        var now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
        var todayBirthdays = (from birthday in Db.Birthdays
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
            : (from s in Db.Stats where s.User.UserId == chatId && s.IsActive select s.ChatId)
            .ToList(); // chatId is a user id
        var usersWhiteList = from s in Db.Stats where requestedChats.Contains(s.ChatId) && s.IsActive select s.User;
        var now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
        var todayBirthdays = (from birthday in Db.Birthdays
                where (birthday.Month == now.Month) & (usersWhiteList == null || usersWhiteList.Contains(birthday.User))
                orderby birthday.Day
                select birthday)
            .Include(birthday => birthday.User)
            .ToList();
        return todayBirthdays;
    }

    public void SetBirthday(string username, int day, int month)
    {
        if (!ValidateDate(day, month)) throw new InvalidBirthdayException();
        var birthday = (from b in Db.Birthdays where b.User.Username == username select b).FirstOrDefault();
        if (birthday == null)
        {
            var user = (from u in Db.Users where u.Username == username select u).FirstOrDefault();
            if (user == null) throw new NoFoundException();
            Db.Birthdays.Add(new Birthday { Day = day, Month = month, User = user });
        }
        else
        {
            birthday.Day = day;
            birthday.Month = month;
        }

        Db.SaveChanges();
    }

    public void SetBirthday(User tgUser, int day, int month)
    {
        if (!ValidateDate(day, month)) throw new InvalidBirthdayException();
        var birthday = (from b in Db.Birthdays where b.User.UserId == tgUser.Id select b).FirstOrDefault();
        if (birthday == null)
        {
            var user = GetOrCreateDbUser(tgUser);
            Db.Birthdays.Add(new Birthday { Day = day, Month = month, User = user });
        }
        else
        {
            birthday.Day = day;
            birthday.Month = month;
        }

        Db.SaveChanges();
    }

    public bool RemoveBirthday(User tgUser)
    {
        var birthday = (from b in Db.Birthdays where b.User.UserId == tgUser.Id select b).FirstOrDefault();
        if (birthday == null) return false;

        Db.Remove(birthday);
        Db.SaveChanges();
        return true;
    }

    private static bool ValidateDate(int day, int month)
    {
        return month switch
        {
            < 1 or > 12 => false,
            2 => day <= 29,
            _ => day > 0 && day <= DateTime.DaysInMonth(2024, month)
        };
    }
}