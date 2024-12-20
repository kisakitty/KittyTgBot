using KittyBot.database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Images;
using OpenAI.Models;
using Serilog;
using Sgbj.Cron;
using Telegram.Bot;
using Telegram.Bot.Types;
using Event = KittyBot.database.Event;
using Message = OpenAI.Chat.Message;

namespace KittyBot.services;

public class EventsNotifier : BackgroundService
{
    private static readonly string AnnounceChatListEnv = "CHATS_WITH_ANNOUNCES";

    private readonly IEnumerable<long> _announceChatList;

    private readonly TelegramBotClient _botClient;
    
    private readonly OpenAIClient _openApiClient;
    
    private readonly IServiceScopeFactory _scopeFactory;
    
    public EventsNotifier(IServiceScopeFactory scopeFactory, TelegramBotClient client, OpenAIClient openApiClient)
    {
        _openApiClient = openApiClient;
        _botClient = client;
        string? _announceChatListString = Environment.GetEnvironmentVariable(AnnounceChatListEnv);
        if (_announceChatListString != null)
        {
            _announceChatList = _announceChatListString.Split(",").Select(long.Parse).ToList();
        }
        else
        {
            _announceChatList = Enumerable.Empty<long>();
        }
        _scopeFactory = scopeFactory;
    }

    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new CronTimer("0 0 * * *", TimeZoneInfo.Utc);
        
        while (await timer.WaitForNextTickAsync())
        {
            AnnounceEvents(stoppingToken);
        }
    }

    private void AnnounceEvents(CancellationToken cancelToken)
    {
        GetTodayEvents().ForEach(async (Event) =>
        {
            var resultAnnounce = await GenerateText(cancelToken, Event);
            
            var request = new ImageGenerationRequest($"Сегодня праздник с названием \"{Event.Title}\". Придумай праздничную картинку к поздравлению, которая отражает особенности данного дня. Описание праздника: {Event.Description}", Model.DallE_3, 1);
            var imageResults = await _openApiClient.ImagesEndPoint.GenerateImageAsync(request, cancelToken);
            

            Log.Information($"Start sending announce:\n{resultAnnounce}");

            foreach (var chatId in _announceChatList)
            {
                Log.Information($"Announce for chat {chatId}");
                try
                {
                    await _botClient.SendPhoto(
                        chatId: chatId,
                        cancellationToken: cancelToken,
                        photo: new InputFileUrl(imageResults[0].Url),
                        caption: resultAnnounce
                    );
                }
                catch (Exception exception)
                {
                    Log.Error(exception, "Error on send telegram message");
                }
            }
        });
    }

    private async Task<string> GenerateText(CancellationToken cancelToken, Event Event)
    {
        var messages = new List<Message>
        {
            new (Role.System, "Ты бот в небольшом чате, основная твоя задача — придумывать поздравления на разные праздники. Придерживайся неформального и молодёжного стиля общения, будь максимально оригинальным и непредсказуемым, используй маты и жаргон. Пользователь напишет тебе имя праздника и его описание. Не пиши ничего кроме поздравления. Максимальная длина поздравления: 950 символов, это очень важно!"),
            new (Role.User, $"Праздник: {Event.Title}. Описание праздника: {Event.Description}"),
        };
        
        var chatRequest = new ChatRequest(messages, Model.GPT4, maxTokens: 400);
        var response = await _openApiClient.ChatEndpoint.GetCompletionAsync(chatRequest, cancelToken);
        string answer = response.FirstChoice.Message.Content.ToString();
        var localDateString = Util.LocalizeDate(new DateTime(2024, Event.Month, Event.Day), "ru-RU");
        return $"{Event.Title} — {localDateString}\n\n{answer}";
    }
    

    private List<Event> GetTodayEvents()
    {
        using var eventsServiceScope = _scopeFactory.CreateScope();
        var eventsService = eventsServiceScope.ServiceProvider.GetRequiredService<EventService>();
        return eventsService.GetTodayEvents();
    }
}