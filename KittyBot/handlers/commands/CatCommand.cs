using System.Net.Http.Json;
using KittyBot.dto;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace KittyBot.handlers.commands;

public class CatCommand : Command
{
    private static readonly HttpClient CatClient = new()
    {
        BaseAddress = new Uri("https://api.thecatapi.com")
    };

    protected override async Task HandleCommand(ITelegramBotClient client, Message message,
        CancellationToken cancelToken)
    {
        var chatId = message.Chat.Id;
        try
        {
            var catLink = await CatClient.GetFromJsonAsync<List<CatLink>>("/v1/images/search", cancelToken);
            if (catLink is { Count: > 0 } && catLink[0].url is not null)
                await client.SendPhoto(
                    chatId,
                    new InputFileUrl(catLink[0].url),
                    cancellationToken: cancelToken,
                    replyParameters: new ReplyParameters { ChatId = chatId, MessageId = message.MessageId }
                );
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error on getting cat's image");
            await client.SendMessage(
                chatId,
                "Какая-то ошибка случилась. Сделай запрос кота ещё раз",
                cancellationToken: cancelToken,
                replyParameters: new ReplyParameters { ChatId = chatId, MessageId = message.MessageId }
            );
        }
    }
}