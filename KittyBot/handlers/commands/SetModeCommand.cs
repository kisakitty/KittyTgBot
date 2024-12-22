using KittyBot.database;
using KittyBot.services;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace KittyBot.handlers.commands;

public class SetModeCommand(IServiceScopeFactory scopeFactory) : Command
{
    protected override async Task HandleCommand(ITelegramBotClient client, Message message,
        CancellationToken cancelToken)
    {
        if (message.From == null) return;
        var chatId = message.Chat.Id;
        var scope = scopeFactory.CreateScope();
        var responseConfigService = scope.ServiceProvider.GetRequiredService<ResponseConfigService>();
        var currentMode = responseConfigService.GetChatMode(chatId);

        var inlineMarkup = new InlineKeyboardMarkup();
        foreach (ChatMode mode in Enum.GetValues(typeof(ChatMode)))
        {
            if (mode == currentMode) continue;
            var modeTitle = mode.ToString();
            inlineMarkup.AddButton(Localizer.GetValue(modeTitle, Locale.RU), mode.ToString());
        }

        inlineMarkup.AddButton("Отмена", "removeMessage");

        await client.SendMessage(
            chatId,
            $"Текущий режим: *{Localizer.GetValue(currentMode.ToString(), Locale.RU)}*\nВыберите новый из списка",
            cancellationToken: cancelToken,
            parseMode: ParseMode.MarkdownV2,
            replyParameters: new ReplyParameters { ChatId = chatId, MessageId = message.MessageId },
            replyMarkup: inlineMarkup
        );
    }
}