using KittyBot.services;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace KittyBot.handlers.commands;

public class RemoveBirthday : Command
{
    private readonly IServiceScopeFactory _scopeFactory;
    
    public RemoveBirthday(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }
    
    protected override async Task HandleCommand(ITelegramBotClient client, Message message, CancellationToken cancelToken)
    {
        if (message.From == null) return;
        var chatId = message.Chat.Id;

        var scope = _scopeFactory.CreateScope();
        var birthdaysService = scope.ServiceProvider.GetRequiredService<BirthdaysService>();
        if (birthdaysService.RemoveBirthday(message.From))
        {
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: "Пока что не буду поздравлять тебя с днём рождения 👌",
                cancellationToken: cancelToken);
        }
        else
        {
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: "Я и так не знаю когда у тебя день рождения 😅",
                cancellationToken: cancelToken);
        }
    }
}