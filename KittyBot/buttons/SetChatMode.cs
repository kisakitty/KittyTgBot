using KittyBot.callbacks;
using KittyBot.database;
using KittyBot.services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace KittyBot.buttons;

public class SetChatMode: CallbackAction
{
    private readonly ResponseConfigService _responseConfigService;
    
    public SetChatMode(ResponseConfigService responseConfigService)
    {
        _responseConfigService = responseConfigService;
    }
    
    public void Handle(ITelegramBotClient client, CallbackQuery callback, CancellationToken cancelToken)
    {
        if (callback.Message == null || callback.Data == null) return;
        _responseConfigService.SetChatMode(callback.Message.Chat.Id, Enum.Parse<ChatMode>(callback.Data));
        client.SendTextMessageAsync(
            chatId: callback.Message.Chat.Id,
            text: $"Режим установлен\\: *{Localizer.GetValue(callback.Data, Locale.RU)}*",
            parseMode: ParseMode.MarkdownV2,
            cancellationToken: cancelToken
        );
        client.DeleteMessageAsync(
            chatId: callback.Message.Chat.Id,
            messageId: callback.Message.MessageId,
            cancellationToken: cancelToken
        );
    }
}