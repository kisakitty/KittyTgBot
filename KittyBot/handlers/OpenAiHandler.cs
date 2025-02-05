using KittyBot.database;
using KittyBot.dto;
using KittyBot.services;
using KittyBot.utility;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Message = OpenAI.Chat.Message;

namespace KittyBot.handlers;

public class OpenAiHandler(IServiceScopeFactory scopeFactory, OpenAIClient openAiClient)
    : Handler
{
    public override async Task HandleUpdate(ITelegramBotClient client, Update update, CancellationToken cancelToken,
        Locale language = Locale.RU)
    {
        await GenerateResponse(client, update, null, cancelToken);
    }


    protected async Task GenerateResponse(ITelegramBotClient client, Update update, long? myId,
        CancellationToken cancelToken)
    {
        if (update.Message?.Text == null) return;


        var formattedMessages = await Util.FormatMessageWithReplies(client, update.Message, myId, cancelToken);
        Log.Debug($"Formatted message: {formattedMessages}");

        var messages = new List<Message>
            {
                new(Role.System,
                    "Ты интерактивный помощник в небольшом чатике. Тебя зовут Хелло Китти! Твои ответы должны быть максимально понятными, простыми и лаконичными. Придерживайся неформального и молодёжного стиля в общении, используй сленг и маты")
            }.Concat(GetHistory(update.Message.Chat.Id))
            .Concat(formattedMessages.Select(msg => new Message(Role.User, msg.text)));

        Log.Information($"Request to OpenAI from {update.Message.From?.Username}");
        var chatRequest = new ChatRequest(messages, Model.GPT3_5_Turbo);
        var response = await openAiClient.ChatEndpoint.GetCompletionAsync(chatRequest, cancelToken);
        var choice = response.FirstChoice;

        var chatId = update.Message.Chat.Id;
        string answer = choice.Message.Content.ToString();
        LogHistoryMessages(formattedMessages, answer, update.Message.Chat.Id);
        using var responseConfigServiceScope = scopeFactory.CreateScope();
        var responseConfigService = responseConfigServiceScope.ServiceProvider.GetRequiredService<ResponseConfigService>();
        var mode = responseConfigService.GetChatMode(chatId);
        LogAnalytics(chatId, "gpt-3.5-turbo", "OpenAI API", mode);
        await client.SendMessage(
            chatId,
            answer,
            cancellationToken: cancelToken,
            linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true },
            replyParameters: new ReplyParameters { ChatId = chatId, MessageId = update.Message.MessageId }
        );
    }

    protected void LogAnalytics(long chatId, string model, string provider, ChatMode mode)
    {
        using var scope = scopeFactory.CreateScope();
        var analyticsService = scope.ServiceProvider.GetRequiredService<AnalyticsService>();
        analyticsService.LogAnalytics(chatId, model, provider, mode);
    }

    private List<Message> GetHistory(long ChatId)
    {
        using var messageServiceScope = scopeFactory.CreateScope();
        var messageService = messageServiceScope.ServiceProvider.GetRequiredService<MessageService>();
        return messageService.GetPreviousMessages(ChatId, 25);
    }

    private void LogHistoryMessages(List<MessageContent> userMessages, string botResponse, long chatId)
    {
        using var messageServiceScope = scopeFactory.CreateScope();
        var messageService = messageServiceScope.ServiceProvider.GetRequiredService<MessageService>();
        userMessages.ForEach(msg =>
        {
            if (msg.text != null)
                messageService.LogMessage(new HistoricalMessage
                    { Content = msg.text, ChatId = chatId, IsBot = false });
        });     
        messageService.LogMessage(new HistoricalMessage { Content = botResponse, ChatId = chatId, IsBot = true });
    }
}