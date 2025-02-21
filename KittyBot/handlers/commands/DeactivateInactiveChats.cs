using KittyBot.services;
using KittyBot.utility;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

namespace KittyBot.handlers.commands;

public class DeactivateInactiveChats(IServiceScopeFactory scopeFactory) : Command
{
    private const int MaxChunkSize = 4096;

    protected override async Task HandleCommand(ITelegramBotClient client, Message message,
        CancellationToken cancelToken)
    {
        var statusMessage = await client.SendMessage(
            message.Chat.Id,
            "Wait...",
            cancellationToken: cancelToken,
            linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true },
            replyParameters: new ReplyParameters { ChatId = message.Chat.Id, MessageId = message.MessageId }
        );
        var scope = scopeFactory.CreateScope();
        var statsService = scope.ServiceProvider.GetRequiredService<StatsService>();
        var activeChatIds = statsService.GetAllActiveChats();
        var activeChats = new List<string>();
        var deactivatedChatsCount = 0;
        var i = 1;
        foreach (var chatId in activeChatIds)
            try
            {
                Thread.Sleep(100);
                var chat = await client.GetChat(chatId, cancelToken);
                if (chat.Title != null && chat.Description != null)
                {
                    activeChats.Add($"{i}. {chat.Title} — {chat.Description}");
                } else if (chat.Title != null)
                {
                    activeChats.Add($"{i}. {chat.Title}");
                }
                else
                {
                    activeChats.Add($"{i}. None");
                }
                ++i;
            }
            catch (ApiRequestException exception)
            {
                if (exception.ErrorCode == 400)
                {
                    ++deactivatedChatsCount;
                    Thread.Sleep(100);
                    statsService.DeactivateChat(chatId);                    
                }
                else
                {
                    Log.Error(exception, "Could not ping chat: {chatId}", chatId);
                }
            }

        var reportMessage =
            $"Количество деактивированных чатов: {deactivatedChatsCount}\n\nАктивные чаты:\n{string.Join("\n", activeChats)}";
        var chunks = StringUtils.SplitTextIntoChunks(reportMessage, MaxChunkSize);
        foreach (var chunk in chunks)
        {
            Thread.Sleep(100);
            await client.SendMessage(
                message.Chat.Id,
                chunk,
                cancellationToken: cancelToken,
                linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true },
                replyParameters: new ReplyParameters { ChatId = message.Chat.Id, MessageId = message.MessageId }
            );
        }

        Thread.Sleep(100);
        await client.EditMessageText(
            new ChatId(statusMessage.Chat.Id),
            statusMessage.MessageId,
            "Done",
            cancellationToken: cancelToken);
    }
}
