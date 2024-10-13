using Telegram.Bot;
using Telegram.Bot.Types;

namespace KittyBot.callbacks;

public interface CallbackAction
{
    public void Handle(ITelegramBotClient client, CallbackQuery callback, CancellationToken cancelToken);
}