namespace KittyBot.database;

public class EventService
{
    private readonly KittyBotContext _db;

    public EventService(KittyBotContext db)
    {
        _db = db;
    }
    
    public List<Event> GetTodayEvents()
    {
        var now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
        return (from even in _db.Events
            where even.Enabled && even.Day == now.Day && even.Month == now.Month
            select even).ToList();
    }
}