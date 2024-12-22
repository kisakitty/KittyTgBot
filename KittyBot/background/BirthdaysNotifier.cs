using KittyBot.database;
using KittyBot.services;
using KittyBot.utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;
using Serilog;
using Sgbj.Cron;
using Telegram.Bot;
using Message = OpenAI.Chat.Message;

namespace KittyBot.background;

public class BirthdaysNotifier(IServiceScopeFactory scopeFactory, TelegramBotClient client, OpenAIClient openApiClient)
    : BackgroundService
{
    private const string BirthdayPrompt =
        "Ты бот в небольшом чате, основная твоя задача — поздравлять пользователей с днём рождения. " +
        "Придерживайся неформального и молодёжного стиля, будь максимально оригинальным и непредсказуемым, используй маты и жаргон. " +
        "Я напишу тебе лишь имя пользователя, его имя и текущий день. Не пиши ничего кроме поздравления. " +
        "Максимальная длина поздравления: 1000 символов!";

    private readonly GeminiBot _geminiBot = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new CronTimer("0 0 * * *", TimeZoneInfo.Local);
        Log.Information("Start birthday notifier");
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            Log.Information("Notifying birthdays");
            AnnounceBirthdays(stoppingToken);
        }
    }

    private void AnnounceBirthdays(CancellationToken cancelToken)
    {
        var birthdays = GetTodayBirthdays();
        if (birthdays.Count == 0) Log.Information("No birthdays found today");
        birthdays.ForEach(async birthday =>
        {
            var userChatList = GetUserChats(birthday.User.Id);
            if (userChatList.Count == 0)
            {
                Log.Information($"No chats for user {birthday.User.Username}");
                return;
            }

            string announce;
            try
            {
                announce = await GenerateText(birthday, cancelToken);
            }
            catch (Exception)
            {
                announce = await _geminiBot.GenerateTextResponse(
                    _geminiBot.BuildMessage(FormatBirthdayRequest(birthday), "user"), "gemini-1.5-flash-001",
                    cancelToken,
                    BirthdayPrompt);
            }

            Log.Information($"Birthday: \n{announce}");
            Log.Information($"Announced chats: {userChatList}");

            foreach (var chatId in userChatList)
            {
                Log.Information($"Announce for chat {chatId}");
                try
                {
                    var chunks = Util.SplitIntoChunks(announce);
                    foreach (var part in chunks)
                    {
                        await client.SendMessage(
                            chatId,
                            cancellationToken: cancelToken,
                            text: part
                        );
                        Thread.Sleep(500);
                    }
                }
                catch (Exception exception)
                {
                    Log.Error(exception, "Error on send telegram message");
                }
            }
        });
    }

    private async Task<string> GenerateText(Birthday birthday, CancellationToken cancelToken)
    {
        var request = FormatBirthdayRequest(birthday);
        var messages = new List<Message>
        {
            new(Role.System, BirthdayPrompt),
            new(Role.User, request)
        };

        var chatRequest = new ChatRequest(messages, Model.GPT4, maxTokens: 400);
        var response = await openApiClient.ChatEndpoint.GetCompletionAsync(chatRequest, cancelToken);
        return response.FirstChoice.Message.Content.ToString();
    }

    private static string FormatBirthdayRequest(Birthday birthday)
    {
        var localDateString = Util.LocalizeDate(DateTime.Now, "ru-RU");
        var request =
            $"Пользователь: {Util.FormatUserName(birthday.User, true, false)}. Имя пользователя: {Util.FormatNames(birthday.User, true)}. Дата: {localDateString}";
        return request;
    }

    private List<Birthday> GetTodayBirthdays()
    {
        using var eventsServiceScope = scopeFactory.CreateScope();
        var eventsService = eventsServiceScope.ServiceProvider.GetRequiredService<BirthdaysService>();
        return eventsService.GetTodayBirthdays();
    }

    private List<long> GetUserChats(long userId)
    {
        using var statsServiceScope = scopeFactory.CreateScope();
        var statsService = statsServiceScope.ServiceProvider.GetRequiredService<StatsService>();
        return statsService.GetUserChats(userId);
    }
}