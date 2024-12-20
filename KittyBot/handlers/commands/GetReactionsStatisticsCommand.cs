using System.Text;
using KittyBot.services;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace KittyBot.handlers.commands;

public class GetReactionsStatisticsCommand : Command
{
    private const int MaxChunkSize = 4096;

    private readonly IServiceScopeFactory _scopeFactory;

    public GetReactionsStatisticsCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task HandleCommand(ITelegramBotClient client, Message message,
        CancellationToken cancelToken)
    {
        var chunks = message.Chat.Id < 0
            ? GetChatStatsByUsers(message)
            : await GetUserStatsByChats(client, message, cancelToken);
        foreach (var chunk in chunks)
        {
            await client.SendMessage(
                message.Chat.Id,
                chunk,
                cancellationToken: cancelToken,
                linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true },
                replyParameters: new ReplyParameters { ChatId = message.Chat.Id, MessageId = message.MessageId },
                parseMode: ParseMode.MarkdownV2
            );
            Thread.Sleep(1000);
        }
    }

    private List<string> GetChatStatsByUsers(Message message)
    {
        using var statsServiceScope = _scopeFactory.CreateScope();
        var reactionsService = statsServiceScope.ServiceProvider.GetRequiredService<ReactionsService>();
        var chunks = new List<string>();
        var sb = new StringBuilder();
        sb.Append("Статистика по реакциям в чатике \\(кто какие реакции получает\\)\\\n\n");
        sb.Append("Пользователь — кол\\-во реакций — самая частая реакция");
        var i = 1;
        reactionsService.GetChatStatistics(message.Chat.Id).ForEach(reacts =>
        {
            var newLine = $"\n{i}\\. {reacts.username} — {reacts.total} — {reacts.topEmoji}";
            ++i;
            if (sb.Length + newLine.Length > MaxChunkSize)
            {
                chunks.Add(sb.ToString());
                sb.Length = 0;
            }

            sb.Append(newLine);
        });
        chunks.Add(sb.ToString());
        return chunks;
    }

    private async Task<List<string>> GetUserStatsByChats(ITelegramBotClient client, Message message, CancellationToken cancelToken)
    {
        using var statsServiceScope = _scopeFactory.CreateScope();
        var reactionsService = statsServiceScope.ServiceProvider.GetRequiredService<ReactionsService>();
        var chunks = new List<string>();
        var sb = new StringBuilder();
        sb.Append("Статистика по реакциям в твоих чатиках\n\n");
        sb.Append("Чат — кол\\-во реакций — самая частая реакция");
        var i = 1;
        var tasks = reactionsService.GetUserStatistics(message.Chat.Id).Select(async reacts =>
        {
            var chat = await client.GetChat(reacts.chatId, cancelToken);
            var title = chat.Title ?? chat.Username ?? chat.FirstName ?? chat.LastName;
            var newLine = $"\n{i}\\. {title} — {reacts.total} — {reacts.topEmoji}";
            ++i;
            return newLine;
        });
        foreach (var res in await Task.WhenAll(tasks))
        {
            if (sb.Length + res.Length > MaxChunkSize)
            {
                chunks.Add(sb.ToString());
                sb.Length = 0;
            }
            sb.Append(res);
        }
        chunks.Add(sb.ToString());
        return chunks;
    }
}