using KittyBot.exceptions;
using KittyBot.services;
using KittyBot.utility;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace KittyBot.handlers.commands;

public class ForceSetBirthdayCommand(IServiceScopeFactory scopeFactory) : Command
{
    protected override async Task HandleCommand(ITelegramBotClient client, Message message,
        CancellationToken cancelToken)
    {
        if (message.Text is null) return;
        var argv = ParseCommand(message.Text);
        var chatId = message.Chat.Id;
        if (argv.Length < 3)
        {
            await FormatError(client, cancelToken, chatId);
            return;
        }

        var username = argv[1];
        var dayMonth = argv[2].Split("-");
        using var scope = scopeFactory.CreateScope();
        var birthdaysService = scope.ServiceProvider.GetRequiredService<BirthdaysService>();
        string localDateString;
        try
        {
            var day = int.Parse(dayMonth[0]);
            var month = int.Parse(dayMonth[1]);
            localDateString = Util.LocalizeDate(new DateTime(2024, month, day), "ru-RU");
            birthdaysService.SetBirthday(RemoveLeadingAt(username), day, month);
        }
        catch (InvalidBirthdayException ex)
        {
            Log.Error(ex, $"Birthday parse error. String: {message.Text}");
            await FormatError(client, cancelToken, chatId);
            return;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Log.Error(ex, $"Birthday parse error. String: {message.Text}");
            await client.SendMessage(
                chatId,
                "Не могу найти такого дня. Проверь корректность даты :(",
                cancellationToken: cancelToken);
            return;
        }
        catch (NoFoundException ex)
        {
            Log.Error(ex, $"Cannot find user with username \"{username}\"");
            await client.SendMessage(
                chatId,
                $"Я не знаю пользователя {username}. Незнакомцев я не поздравляю :(",
                cancellationToken: cancelToken);
            return;
        }

        await client.SendMessage(
            chatId,
            $"Всё ок. Теперь я знаю что день рожденя пользователя {username} наступит {localDateString}!",
            cancellationToken: cancelToken);
    }

    private static string RemoveLeadingAt(string username)
    {
        if (username.Length == 0) return username;
        return username[0].Equals('@') ? username[1..] : username;
    }

    private static async Task FormatError(ITelegramBotClient client, CancellationToken cancelToken, long chatId)
    {
        await client.SendMessage(
            chatId,
            "Не могу распарсить\\. Пришли в формате `/setbd @user DD-MM`",
            cancellationToken: cancelToken,
            parseMode: ParseMode.MarkdownV2);
    }
}