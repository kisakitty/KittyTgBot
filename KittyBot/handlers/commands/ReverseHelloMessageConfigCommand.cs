using KittyBot.services;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace KittyBot.handlers.commands;

public class ReverseHelloMessageConfigCommand: Command
{
    private readonly IServiceScopeFactory _scopeFactory;
    
    public ReverseHelloMessageConfigCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }
    
    protected override async Task HandleCommand(ITelegramBotClient client, Message message, CancellationToken cancelToken)
    {
        if (message.From == null) return;
        var chatId = message.Chat.Id;

        if (chatId > 0)
        {
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: "В приватном чате эта команда бессмысленна",
                cancellationToken: cancelToken);
            return;
        }
        var chatAdministrators = await client.GetChatAdministratorsAsync(chatId, cancelToken);
        if (chatAdministrators.All(admin => admin.User.Id != message.From.Id)) 
        {
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: "Ты не админ! Попроси кого-нибудь с плашкой запустить эту команду",
                cancellationToken: cancelToken);
            return;
        }
        var scope = _scopeFactory.CreateScope();
        var responseConfigService = scope.ServiceProvider.GetRequiredService<ResponseConfigService>();
        if (responseConfigService.ReverseHelloMessageStatus(chatId))
        {
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: "Теперь буду приветствовать от всего процессорного сердца каждого нового участника!",
                cancellationToken: cancelToken);
        }
        else
        {
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: "Пока не буду отпугивать новых учатников своим трёпом 👌",
                cancellationToken: cancelToken);
        }
    }
}