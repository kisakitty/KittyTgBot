using KittyBot.database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;
using Serilog;
using Sgbj.Cron;
using Telegram.Bot;
using Message = OpenAI.Chat.Message;

namespace KittyBot.services;

public class BirthdaysNotifier : BackgroundService
{
    private static readonly string AnnounceChatListEnv = "CHATS_WITH_ANNOUNCES";

    private readonly IEnumerable<long> _announceChatList;

    private readonly TelegramBotClient _botClient;
    
    private readonly OpenAIClient _openApiClient;
    
    private readonly IServiceScopeFactory _scopeFactory;
    
    public BirthdaysNotifier(IServiceScopeFactory scopeFactory, TelegramBotClient client, OpenAIClient openApiClient)
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
        using var timer = new CronTimer("0 0 * * *", TimeZoneInfo.Local);
        while (await timer.WaitForNextTickAsync())
        {
            AnnounceBirthdays(stoppingToken);
        }
    }

    private void AnnounceBirthdays(CancellationToken cancelToken)
    {
        GetTodayBirthdays().ForEach(async birthday =>
        {
            var userChatList = GetUserChats(birthday.User.Id);
            if (userChatList.Count == 0)
            {
                return;
            }
            var announce = await GenerateText(cancelToken, birthday);
            Log.Information($"Birthday: \n{announce}");
            Log.Information($"Announced chats: {userChatList}");
            
            foreach (var chatId in userChatList)
            {
                Log.Information($"Announce for chat {chatId}");
                try
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        cancellationToken: cancelToken,
                        text: announce
                    );
                }
                catch (Exception exception)
                {
                    Log.Error(exception, "Error on send telegram message");
                }
            }

        });
    }
    
    private async Task<string> GenerateText(CancellationToken cancelToken, Birthday birthday)
    {
        var localDateString = Util.LocalizeDate(DateTime.Now, "ru-RU");
        var messages = new List<Message>
        {
            new (Role.System, "Ты бот в небольшом чате, основная твоя задача — поздравлять пользователей с днём рождения. " +
                              "Придерживайся неформального и молодёжного стиля, будь максимально оригинальным и непредсказуемым, используй маты и жаргон. " +
                              "Я напишу тебе лишь имя пользователя, его имя и текущий день. Не пиши ничего кроме поздравления. " +
                              "Максимальная длина поздравления: 1000 символов!"),
            new (Role.User, $"Пользователь: {Util.FormatUserName(birthday.User, true)}. Имя пользователя: {Util.FormatNames(birthday.User)}. Дата: {localDateString}"),
        };
        
        var chatRequest = new ChatRequest(messages, Model.GPT4, maxTokens: 400);
        var response = await _openApiClient.ChatEndpoint.GetCompletionAsync(chatRequest, cancelToken);
        return response.FirstChoice.Message.Content.ToString();
    }
    
    private List<Birthday> GetTodayBirthdays()
    {
        using var eventsServiceScope = _scopeFactory.CreateScope();
        var eventsService = eventsServiceScope.ServiceProvider.GetRequiredService<BirthdaysService>();
        return eventsService.GetTodayBirthdays();
    }

    private List<long> GetUserChats(long userId)
    {
        using var statsServiceScope = _scopeFactory.CreateScope();
        var statsService = statsServiceScope.ServiceProvider.GetRequiredService<StatsSerivce>();
        return statsService.GetUserChats(userId);
    }
}