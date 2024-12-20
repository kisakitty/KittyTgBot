using KittyBot.callbacks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace KittyBot.buttons;

public class RemoveMessage: CallbackAction
{
    public void Handle(ITelegramBotClient client, CallbackQuery callback, CancellationToken cancelToken)
    {
        if (callback.Message == null) return;
        client.DeleteMessage(
            chatId: callback.Message.Chat.Id,
            messageId: callback.Message.MessageId,
            cancellationToken: cancelToken
        );
    }
}