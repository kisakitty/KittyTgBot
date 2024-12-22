using KittyBot.database;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace KittyBot.handlers.commands;

public class HelpCommand : Command
{
    protected override async Task HandleCommand(ITelegramBotClient client, Message message,
        CancellationToken cancelToken)
    {
        await client.SendMessage(
            message.Chat.Id,
            Localizer.GetValue("Help", Locale.RU),
            cancellationToken: cancelToken,
            linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true },
            replyParameters: new ReplyParameters { ChatId = message.Chat.Id, MessageId = message.MessageId },
            parseMode: ParseMode.MarkdownV2
        );
    }
}