using KittyBot.database;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace KittyBot.handlers.commands;

public class StartCommand: Command
{
    protected override async Task HandleCommand(ITelegramBotClient client, Message message, CancellationToken cancelToken)
    {
        var chatId = message.Chat.Id;
        await client.SendMessage(
            chatId: chatId,
            text: Localizer.GetValue("Start", Locale.RU),
            cancellationToken: cancelToken);
    }
}