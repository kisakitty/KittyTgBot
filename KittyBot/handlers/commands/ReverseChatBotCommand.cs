using KittyBot.services;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace KittyBot.handlers.commands;

public class ReverseChatBotCommand: Command
{
    private readonly IServiceScopeFactory _scopeFactory;
    
    public ReverseChatBotCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }
    
    protected override async Task HandleCommand(ITelegramBotClient client, Message message, CancellationToken cancelToken)
    {
        if (message.From == null) return;
        var chatId = message.Chat.Id;

        if (chatId > 0)
        {
            await client.SendMessage(
                chatId: chatId,
                text: "В приватном чате эта команда бессмысленна",
                cancellationToken: cancelToken);
            return;
        }
        var chatAdministrators = await client.GetChatAdministrators(message.Chat.Id, cancelToken);
        if (chatAdministrators.All(admin => admin.User.Id != message.From.Id)) 
        {
            await client.SendMessage(
                chatId: chatId,
                text: "Ты не админ! Попроси кого-нибудь с плашкой запустить эту команду",
                cancellationToken: cancelToken);
            return;
        }
        var scope = _scopeFactory.CreateScope();
        var responseConfigService = scope.ServiceProvider.GetRequiredService<ResponseConfigService>();
        if (responseConfigService.ReverseChatBotStatus(chatId))
        {
            await client.SendMessage(
                chatId: chatId,
                text: "Я снова здесь! Позови меня (по никнейму или просто \"бот\") и я приду!",
                cancellationToken: cancelToken);
        }
        else
        {
            await client.SendMessage(
                chatId: chatId,
                text: "Заткнулся 🤐",
                cancellationToken: cancelToken);
        }
    }
}