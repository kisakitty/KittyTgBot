using System.Text;
using KittyBot.services;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace KittyBot.handlers.commands;

public class GetAnalyticsCommand(IServiceScopeFactory scopeFactory) : Command
{
    protected override async Task HandleCommand(ITelegramBotClient client, Message message,
        CancellationToken cancelToken)
    {
        var chatId = message.Chat.Id;
        using var scope = scopeFactory.CreateScope();
        var analyticsService = scope.ServiceProvider.GetRequiredService<AnalyticsService>();
        var sb = new StringBuilder();
        var modelAnalytics = analyticsService.GetAnalytics(chatId);
        if (modelAnalytics.Count == 0)
        {
            await client.SendMessage(
                chatId,
                "Никакх запросов к LLM не было в этом чатике",
                cancellationToken: cancelToken,
                linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true },
                replyParameters: new ReplyParameters { ChatId = chatId, MessageId = message.MessageId }
            );
            return;
        }

        sb.Append("Статистика по используемым в этом чате моделям\n");
        sb.Append("Модель — провайдер — количество запросов\n");
        modelAnalytics.ForEach(analytic =>
        {
            var newLine = $"\n{analytic.Model} — {analytic.Provider} — {analytic.CountRequests}";
            sb.Append(newLine);
        });

        await client.SendMessage(
            chatId,
            sb.ToString(),
            cancellationToken: cancelToken,
            linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true },
            replyParameters: new ReplyParameters { ChatId = chatId, MessageId = message.MessageId }
        );
    }
}