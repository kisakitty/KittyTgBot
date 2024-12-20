using System.Text;
using KittyBot.services;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace KittyBot.handlers.commands;

public class GetStatsCommand : Command
{
    private const int MaxChunkSize = 4096;

    private readonly IServiceScopeFactory _scopeFactory;

    public GetStatsCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }
    
    protected override async Task HandleCommand(ITelegramBotClient client, Message message, CancellationToken cancelToken)
    {
        using var statsServiceScope = _scopeFactory.CreateScope();
        var statsService = statsServiceScope.ServiceProvider.GetRequiredService<StatsService>();

        var sb = new StringBuilder();
        sb.Append("Статистика по сообщениям\n");
        var chunks = new List<string>();

        var i = 1;
        statsService.GetGlobalStatsLinks(message.Chat.Id, false).ForEach(stats =>
        {
            var newLine = $"\n{i}\\. {stats.Key} — {stats.Value}";
            ++i;
            if (sb.Length + newLine.Length > MaxChunkSize)
            {
                chunks.Add(sb.ToString());
                sb.Length = 0;
            }
            sb.Append(newLine);
        });
        chunks.Add(sb.ToString());
        foreach (var chunk in chunks)
        {
            await client.SendMessage(
                chatId: message.Chat.Id,
                text: chunk,
                cancellationToken: cancelToken,
                linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true },
                replyParameters: new ReplyParameters { ChatId = message.Chat.Id, MessageId = message.MessageId },
                parseMode: ParseMode.MarkdownV2
            );   
            Thread.Sleep(1000);
        }
    }
}