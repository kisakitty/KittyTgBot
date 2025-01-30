using KittyBot.services;
using KittyBot.utility;
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

namespace KittyBot.background;

public class EventsNotifier : BackgroundService
{
    private const string EventsPrompt =
        "Ты бот в небольшом чате, основная твоя задача — придумывать поздравления на разные праздники. Придерживайся неформального и молодёжного стиля общения, будь максимально оригинальным и непредсказуемым, используй маты и жаргон. Пользователь напишет тебе имя праздника и его описание. Не пиши ничего кроме поздравления. Максимальная длина поздравления: 950 символов, это очень важно!";

    private const int MaxCaptionSize = 1024;
    
    private const string AnnounceChatListEnv = "CHATS_WITH_ANNOUNCES";

    private readonly IEnumerable<long> _announceChatList;

    private readonly TelegramBotClient _botClient;

    private readonly GeminiBot _geminiBot = new();

    private readonly OpenAIClient _openApiClient;

    private readonly IServiceScopeFactory _scopeFactory;

    public EventsNotifier(IServiceScopeFactory scopeFactory, TelegramBotClient client, OpenAIClient openApiClient)
    {
        _openApiClient = openApiClient;
        _botClient = client;
        var announceChatListString = Environment.GetEnvironmentVariable(AnnounceChatListEnv);
        _announceChatList = announceChatListString?.Split(",").Select(long.Parse).ToList() ?? Enumerable.Empty<long>();
        _scopeFactory = scopeFactory;
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new CronTimer("0 0 * * *", TimeZoneInfo.Utc);

        while (await timer.WaitForNextTickAsync(stoppingToken)) AnnounceEvents(stoppingToken);
    }

    private void AnnounceEvents(CancellationToken cancelToken)
    {
        GetTodayEvents().ForEach(async @event =>
        {
            string resultAnnounce;
            try
            {
                resultAnnounce = await GenerateText(@event, cancelToken);
            }
            catch (Exception)
            {
                resultAnnounce = await _geminiBot.GenerateTextResponse(
                    _geminiBot.BuildMessage(FormatEventRequest(@event), "user"), "gemini-1.5-flash-001",
                    cancelToken,
                    EventsPrompt);
            }

            var request = new ImageGenerationRequest(
                $"Сегодня праздник с названием \"{@event.Title}\". Придумай праздничную картинку к поздравлению, которая отражает особенности данного дня. Описание праздника: {@event.Description}",
                Model.DallE_3);
            var imageResults = await _openApiClient.ImagesEndPoint.GenerateImageAsync(request, cancelToken);


            Log.Information($"Start sending announce:\n{resultAnnounce}");

            foreach (var chatId in _announceChatList)
            {
                Log.Information($"Announce for chat {chatId}");
                try
                {
                    await _botClient.SendPhoto(
                        chatId,
                        cancellationToken: cancelToken,
                        photo: new InputFileUrl(imageResults[0].Url),
                        caption: resultAnnounce[..MaxCaptionSize]
                    );
                }
                catch (Exception exception)
                {
                    Log.Error(exception, "Error on send telegram message");
                }
            }
        });
    }

    private async Task<string> GenerateText(Event @event, CancellationToken cancelToken)
    {
        var messages = new List<Message>
        {
            new(Role.System, EventsPrompt),
            new(Role.User, FormatEventRequest(@event))
        };

        var chatRequest = new ChatRequest(messages, Model.GPT4, maxTokens: 400);
        var response = await _openApiClient.ChatEndpoint.GetCompletionAsync(chatRequest, cancelToken);
        string answer = response.FirstChoice.Message.Content.ToString();
        var localDateString = Util.LocalizeDate(new DateTime(2024, @event.Month, @event.Day), "ru-RU");
        return $"{@event.Title} — {localDateString}\n\n{answer}";
    }

    private static string FormatEventRequest(Event @event)
    {
        var request = $"Праздник: {@event.Title}. Описание праздника: {@event.Description}";
        return request;
    }


    private List<Event> GetTodayEvents()
    {
        using var eventsServiceScope = _scopeFactory.CreateScope();
        var eventsService = eventsServiceScope.ServiceProvider.GetRequiredService<EventService>();
        return eventsService.GetTodayEvents();
    }
}