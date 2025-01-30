using KittyBot.database;

namespace KittyBot.services;

public class EventService(KittyBotContext db) : BaseService(db)
{
    public List<Event> GetTodayEvents()
    {
        var now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
        return (from even in Db.Events
            where even.Enabled && even.Day == now.Day && even.Month == now.Month
            select even).ToList();
    }
}