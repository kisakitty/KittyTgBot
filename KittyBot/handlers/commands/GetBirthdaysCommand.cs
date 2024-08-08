using System.Text;
using KittyBot.services;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace KittyBot.handlers.commands;

public class GetBirthdaysCommand : Command
{
    private readonly IServiceScopeFactory _scopeFactory;

    public GetBirthdaysCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task HandleCommand(ITelegramBotClient client, Message message, CancellationToken cancelToken)
    {
        var chatId = message.Chat.Id;
        var scope = _scopeFactory.CreateScope();
        var birthdaysService = scope.ServiceProvider.GetRequiredService<BirthdaysService>();
        var birthdays = birthdaysService.GetBirthdaysThisMonth(message.Chat.Id);
        if (birthdays.Count == 0)
        {
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: "В этом месяце никто не отмечает день рождения 😔",
                cancellationToken: cancelToken);
            return;
        }

        var sb = new StringBuilder();
        sb.Append("Именинники/именинницы этого месяца\\!\n\n");
        birthdays.ForEach(birthday =>
        {
            var localDateString = Util.LocalizeDate(new DateTime(2024, birthday.Month, birthday.Day), "ru-RU");
            sb.Append($"{Util.FormatUserName(birthday.User, false)} — {localDateString}\n");
        });
        await client.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: sb.ToString(),
            cancellationToken: cancelToken,
            linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true },
            replyParameters: new ReplyParameters { ChatId = chatId, MessageId = message.MessageId },
            parseMode: ParseMode.MarkdownV2
        );
    }
}