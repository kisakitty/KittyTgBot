using KittyBot.services;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;

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
        messageService.ClearChatMessages(chatId);
        await client.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "История общения со мной очищена 👌",
            cancellationToken: cancelToken,
            linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true },
            replyParameters: new ReplyParameters { ChatId = chatId, MessageId = message.MessageId }
        );
    }
}