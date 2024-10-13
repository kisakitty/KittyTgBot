using KittyBot.database;

namespace KittyBot.services;

public class AnalyticsService
{
    private readonly KittyBotContext _db;

    public AnalyticsService(KittyBotContext db)
    {
        _db = db;
    }

    public void LogAnalytics(long chatId, string model, string provider, ChatMode mode)
    {
        var currentAnalytics = (from analytic in _db.ModelsAnalytics
            where analytic.ChatId == chatId && analytic.Model == model && analytic.Provider == provider && analytic.ChatMode == mode
            select analytic)
            .FirstOrDefault();
        if (currentAnalytics == null)
        {
            _db.ModelsAnalytics.Add(new ModelAnalytic { ChatId = chatId, Model = model, Provider = provider, CountRequests = 1L, ChatMode = mode });
        }
        else
        {
            currentAnalytics.CountRequests += 1;
        }

        _db.SaveChanges();
    }

    public List<ModelAnalytic> GetAnalytics(long chatId)
    {
        var analytics = from a in _db.ModelsAnalytics
            orderby a.CountRequests descending 
            where a.ChatId == chatId
            select a;
        return analytics
            .ToList();
    }
}