using Telegram.Bot;
using Telegram.Bot.Types;

namespace KittyBot.buttons;

public interface ICallbackAction
{
    public void Handle(ITelegramBotClient client, CallbackQuery callback, CancellationToken cancelToken);
}