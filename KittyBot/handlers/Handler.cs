using KittyBot.database;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace KittyBot.handlers;

public abstract class Handler
{
    public abstract Task HandleUpdate(ITelegramBotClient client, Update update, CancellationToken cancelToken, Locale language = Locale.RU);
}