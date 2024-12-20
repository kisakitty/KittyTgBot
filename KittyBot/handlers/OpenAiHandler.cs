using KittyBot.database;
using KittyBot.services;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Message = OpenAI.Chat.Message;

namespace KittyBot.handlers;

public class OpenAiHandler: Handler
{

    private readonly OpenAIClient _openAiClient;
    
    private readonly IServiceScopeFactory _scopeFactory;

    public OpenAiHandler(IServiceScopeFactory scopeFactory, OpenAIClient openAiClient)
    {
        _scopeFactory = scopeFactory;
        _openAiClient = openAiClient;
    }



    public override async Task HandleUpdate(ITelegramBotClient client, Update update, CancellationToken cancelToken,
        Locale language = Locale.RU)
    {
        await GenerateResponse(client, update, cancelToken, null);
    }


    public async Task GenerateResponse(ITelegramBotClient client, Update update, CancellationToken cancelToken, long? myId)
    {
        if (update.Message?.Text == null) return;


        var formattedMessage = Util.FormatMessage(update.Message, myId);
        Log.Debug($"Formatted message: {formattedMessage}");

        var messages = new List<Message>
        {
            new(Role.System,
                "Ты интерактивный помощник в небольшом чатике. Тебя зовут Хелло Китти! Твои ответы должны быть максимально понятными, простыми и лаконичными. Придерживайся неформального и молодёжного стиля в общении, используй сленг и маты")
        }.Concat(GetHistory(update.Message.Chat.Id))
        .Append(new Message(Role.User, formattedMessage));
        
        Log.Information($"Request to OpenAI from {update.Message.From?.Username}");
        var chatRequest = new ChatRequest(messages, Model.GPT3_5_Turbo);
        var response = await _openAiClient.ChatEndpoint.GetCompletionAsync(chatRequest, cancelToken);
        var choice = response.FirstChoice;
        
        long chatId = update.Message.Chat.Id;
        string answer = choice.Message.Content.ToString();
        LogHistoryMessages(formattedMessage, answer, update.Message.Chat.Id);
        using var responseConfigServiceScope = _scopeFactory.CreateScope();
        var responseConfigService = responseConfigServiceScope.ServiceProvider.GetRequiredService<ResponseConfigService>();
        var mode = responseConfigService.GetChatMode(chatId);
        LogAnalytics(chatId, "gpt-3.5-turbo", "OpenAI API", mode);
        await client.SendMessage(
            chatId: chatId,
            text: answer,
            cancellationToken: cancelToken,
            linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true},
            replyParameters: new ReplyParameters { ChatId = chatId, MessageId = update.Message.MessageId }
        );
    }

    protected void LogAnalytics(long chatId, string model, string provider, ChatMode mode)
    {
        using var scope = _scopeFactory.CreateScope();
        var analyticsService = scope.ServiceProvider.GetRequiredService<AnalyticsService>();
        analyticsService.LogAnalytics(chatId, model, provider, mode);
    }

    private List<Message> GetHistory(long ChatId)
    {
        using var messageServiceScope = _scopeFactory.CreateScope();
        var messageService = messageServiceScope.ServiceProvider.GetRequiredService<MessageService>();
        return messageService.GetPreviousMessages(ChatId, 25);
    }

    private void LogHistoryMessages(string userMessage, string botResponse, long chatId)
    {
        using var messageServiceScope = _scopeFactory.CreateScope();
        var messageService = messageServiceScope.ServiceProvider.GetRequiredService<MessageService>();
        messageService.LogMessage(new HistoricalMessage { Content = userMessage, ChatId = chatId, IsBot = false });
        messageService.LogMessage(new HistoricalMessage { Content = botResponse, ChatId = chatId, IsBot = true });
    }
}