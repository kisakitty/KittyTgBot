using Telegram.Bot;
using Telegram.Bot.Types;

namespace KittyBot.buttons;

public class RemoveMessage : ICallbackAction
{
    public void Handle(ITelegramBotClient client, CallbackQuery callback, CancellationToken cancelToken)
    {
        if (callback.Message == null) return;
        client.DeleteMessage(
            callback.Message.Chat.Id,
            callback.Message.MessageId,
            cancelToken
        );
    }
}