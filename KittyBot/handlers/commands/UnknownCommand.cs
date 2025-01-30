using Telegram.Bot;
using Telegram.Bot.Types;

namespace KittyBot.handlers.commands;

public class UnknownCommand : Command
{
    protected override Task HandleCommand(ITelegramBotClient client, Message message, CancellationToken cancelToken)
    {
        return Task.CompletedTask;
    }
}