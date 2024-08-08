using KittyBot.database;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace KittyBot.handlers.commands;

public abstract class Command: Handler
{
    public override async Task HandleUpdate(ITelegramBotClient client, Update update, CancellationToken cancelToken, Locale language = Locale.RU)
    {
        if (update.Message == null) return;
        await HandleCommand(client, update.Message, cancelToken);
    }
    
    protected abstract Task HandleCommand(ITelegramBotClient client, Message message, CancellationToken cancelToken);
    
    protected static string[] ParseCommand(string fullCommandMessage)
    {
        return fullCommandMessage.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    }
}