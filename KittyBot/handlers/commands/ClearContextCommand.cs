using KittyBot.database;
using KittyBot.services;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace KittyBot.handlers.commands;

public class ClearContextCommand: Command
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ClearContextCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }
    
    protected override async Task HandleCommand(ITelegramBotClient client, Message message, CancellationToken cancelToken)
    {
        var scope = _scopeFactory.CreateScope();
        var messageService = scope.ServiceProvider.GetRequiredService<MessageService>();
        var chatId = message.Chat.Id;
        var responseConfigService = scope.ServiceProvider.GetRequiredService<ResponseConfigService>();
        var currentMode = responseConfigService.GetChatMode(chatId);
        messageService.ClearChatMessages(chatId, currentMode);
        await client.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"История общения со мной очищена \\(в рамках режима *{Localizer.GetValue(currentMode.ToString(), Locale.RU)}*\\) 👌",
            cancellationToken: cancelToken,
            parseMode: ParseMode.MarkdownV2,
            linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true },
            replyParameters: new ReplyParameters { ChatId = chatId, MessageId = message.MessageId }
        );
    }
}