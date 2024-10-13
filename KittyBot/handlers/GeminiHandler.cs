using KittyBot.database;
using KittyBot.dto.gemini;
using KittyBot.exceptions;
using KittyBot.services;
using KittyBot.utility;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace KittyBot.handlers;

public class GeminiHandler(IServiceScopeFactory scopeFactory) : Handler
{
    public const string GeminiApiKeyEnv = "GOOGLE_API_KEY";

    private readonly GeminiBot _geminiBot = new();

    public override async Task HandleUpdate(ITelegramBotClient client, Update update, CancellationToken cancelToken, Locale language = Locale.RU)
    {
        await GenerateResponse(client, update, null, cancelToken, language);
    }
    
    public async Task GenerateResponse(ITelegramBotClient client, Update update, long? myId, CancellationToken cancelToken, Locale language = Locale.RU)
    {
        if (update.Message is null) return;
        var formattedMessage = Util.FormatMessage(update.Message, myId);
        var photo = await Util.GetPhotoBase64(client, update.Message, cancelToken);
        if (update.Message.Text is null && update.Message.Caption is null && photo is null) return;
        Log.Debug($"Formatted message: {formattedMessage}");
        var chatId = update.Message.Chat.Id;
        try
        {
            client.SendChatActionAsync(
                chatId: chatId,
                action: ChatAction.Typing,
                cancellationToken: cancelToken
            );
            using var responseConfigServiceScope = scopeFactory.CreateScope();
            var responseConfigService = responseConfigServiceScope.ServiceProvider.GetRequiredService<ResponseConfigService>();
            var mode = responseConfigService.GetChatMode(chatId);
            var messageContent = photo is null
                ? await GenerateResponseText(update.Message.Chat.Id, formattedMessage, mode, cancelToken)
                : await GenerateResponseImage(formattedMessage, photo, cancelToken);
            
            LogHistoryMessages(formattedMessage, messageContent, update.Message.Chat.Id, mode);
            LogAnalytics(chatId, photo is null ? "gemini-pro" : "gemini-pro-vision", "Google API", mode);
            
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: messageContent,
                cancellationToken: cancelToken,
                linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true},
                replyParameters: new ReplyParameters { ChatId = chatId, MessageId = update.Message.MessageId }
            );
        }
        catch (GeminiException ex)
        {
            Log.Error(ex, $"Can't use Gemini API");
            Log.Error(ex.Message);
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: "Не могу придумать ответ. Напиши ещё раз",
                cancellationToken: cancelToken,
                linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true},
                replyParameters: new ReplyParameters { ChatId = chatId, MessageId = update.Message.MessageId }
            );
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Cannot send telegram message");
        }
    }
    

    private async Task<string> GenerateResponseImage(string formattedMessage, string photoBase64, CancellationToken cancelToken)
    {
        var contents = new List<GeminiMessage> { new(new List<GeminiContent> { new(formattedMessage, null), new(null, new GeminiInlineData("image/jpeg", photoBase64)) }, null) };
        return await _geminiBot.GenerateTextResponse(contents, "gemini-pro-vision", cancelToken);
    }

    private async Task<string> GenerateResponseText(long chatId, string formattedMessage, ChatMode mode, CancellationToken cancelToken)
    {
        var contents = PromptMapper.GetGeminiPromptMessages(mode)
                .Concat(GetHistory(chatId, mode))
            .Append(new GeminiMessage(new List<GeminiContent> { new(formattedMessage, null) }, "user"))
            .ToList();
        return await _geminiBot.GenerateTextResponse(contents, "gemini-pro", cancelToken);
    }
    
    private List<GeminiMessage> GetHistory(long chatId, ChatMode mode)
    {
        using var messageServiceScope = scopeFactory.CreateScope();
        var messageService = messageServiceScope.ServiceProvider.GetRequiredService<MessageService>();
        return messageService.GetPreviousMessagesGemini(chatId, 25, mode);
    }
    
    private void LogHistoryMessages(string userMessage, string botResponse, long chatId, ChatMode mode)
    {
        using var messageServiceScope = scopeFactory.CreateScope();
        var messageService = messageServiceScope.ServiceProvider.GetRequiredService<MessageService>();
        messageService.LogMessage(new HistoricalMessage { Content = userMessage, ChatId = chatId, IsBot = false, ChatMode = mode});
        messageService.LogMessage(new HistoricalMessage { Content = botResponse, ChatId = chatId, IsBot = true, ChatMode = mode });
    }
    
    private void LogAnalytics(long chatId, string model, string provider, ChatMode mode)
    {
        using var scope = scopeFactory.CreateScope();
        var analyticsService = scope.ServiceProvider.GetRequiredService<AnalyticsService>();
        analyticsService.LogAnalytics(chatId, model, provider, mode);
    }
}