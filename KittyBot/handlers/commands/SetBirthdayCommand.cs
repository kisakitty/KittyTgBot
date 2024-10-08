using System.Text.RegularExpressions;
using KittyBot.services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace KittyBot.handlers.commands;

public partial class SetBirthdayCommand : Command
{
    private readonly IServiceScopeFactory _scopeFactory;
    
    public SetBirthdayCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }
    
    protected override async Task HandleCommand(ITelegramBotClient client, Message message, CancellationToken cancelToken)
    {
        if (message.Text == null || message.From == null) return;
        var words = message.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var chatId = message.Chat.Id;
        if (words.Length < 2 || !BirthdayRegexp().Match(words[1]).Success)
        {
            await FormatError(client, cancelToken, chatId);
            return;
        }

        var dayMonth = words[1].Split("-");
        
        using var scope = _scopeFactory.CreateScope();
        var birthdaysService = scope.ServiceProvider.GetRequiredService<BirthdaysService>();
        string localDateString;
        try
        {
            var day = int.Parse(dayMonth[0]);
            var month = int.Parse(dayMonth[1]);
            localDateString = Util.LocalizeDate(new DateTime(2024, month, day), "ru-RU");
            birthdaysService.SetBirthday(message.From, day, month);
        } catch (Exception ex)
        {
            Log.Error(ex, $"Birthday parse error. String: {message.Text}");
            await FormatError(client, cancelToken, chatId);
            return;
        }
        await client.SendTextMessageAsync(
            chatId: chatId,
            text: $"Всё ок. Теперь я знаю что твой день рожденя наступит {localDateString}!",
            cancellationToken: cancelToken);
    }

    private static async Task FormatError(ITelegramBotClient client, CancellationToken cancelToken, long chatId)
    {
        await client.SendTextMessageAsync(
            chatId: chatId,
            text: "Не могу распарсить\\. Добавь свой день рождение в формате `/setbirthday DD-MM`",
            cancellationToken: cancelToken,
            parseMode: ParseMode.MarkdownV2);
    }

    [GeneratedRegex("^\\d{1,2}-\\d{1,2}$")]
    private static partial Regex BirthdayRegexp();
}