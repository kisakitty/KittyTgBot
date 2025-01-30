using KittyBot.database;
using KittyBot.services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace KittyBot.buttons;

public class SetChatMode(ResponseConfigService responseConfigService) : ICallbackAction
{
    public void Handle(ITelegramBotClient client, CallbackQuery callback, CancellationToken cancelToken)
    {
        if (callback.Message == null || callback.Data == null) return;
        responseConfigService.SetChatMode(callback.Message.Chat.Id, Enum.Parse<ChatMode>(callback.Data));
        client.SendMessage(
            callback.Message.Chat.Id,
            $"Режим установлен\\: *{Localizer.GetValue(callback.Data, Locale.RU)}*",
            ParseMode.MarkdownV2,
            cancellationToken: cancelToken
        );
        client.DeleteMessage(
            callback.Message.Chat.Id,
            callback.Message.MessageId,
            cancelToken
        );
    }
}