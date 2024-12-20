using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KittyBot.database;
using KittyBot.dto;
using KittyBot.services;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace KittyBot.handlers;

internal record ModelTimeout(string Model, string?  Provider, TimeSpan Timeout);

public class Gpt4FreeHandler: OpenAiHandler

{
    private const string Gpt4FreeEndpointEnv = "GPT_4_FREE_ENDPOINT";
    
    private readonly string _endpoint;
    
    private static readonly List<ModelTimeout> Models = new()
    {
        new ModelTimeout("gpt-3.5-turbo", "OpenaiChat", new TimeSpan(0, 0, 30))
    };
    
    private readonly IServiceScopeFactory _scopeFactory;

    public Gpt4FreeHandler(IServiceScopeFactory scopeFactory, OpenAIClient openAiClient) : base(scopeFactory, openAiClient)
    {
        _endpoint = Environment.GetEnvironmentVariable(Gpt4FreeEndpointEnv) ??
                    throw new EnvVariablesException($"Expect GPT4Free endpoint. Set it to environment variable {Gpt4FreeEndpointEnv}");
        _scopeFactory = scopeFactory;
    }

    public override async Task HandleUpdate(ITelegramBotClient client, Update update, CancellationToken cancelToken, Locale language = Locale.RU)
    {
        await TryGenerateFreeResponse(client, update, null, cancelToken);
    }

    public async Task TryGenerateFreeResponse(ITelegramBotClient client, Update update, long? myId, CancellationToken cancelToken)
    {
        if (update.Message?.Text == null) return;

        var formattedMessage = Util.FormatMessage(update.Message, myId);
        Log.Debug($"Formatted message: {formattedMessage}");
        
        var messages = new List<GPT4FreeMessage>
            {
                new("system",
                    "Ты интерактивный помощник в небольшом чатике. Тебя зовут Хелло Китти! Твои ответы должны быть максимально понятными, простыми и лаконичными. Придерживайся неформального и молодёжного стиля в общении, используй сленг и маты")
            }.Concat(GetHistory(update.Message.Chat.Id))
            .Append(new GPT4FreeMessage("user", formattedMessage))
            .ToList();
        var modelIndex = 0;
        GPT4FreeResponse? response = null;
        response = await GetResponse(cancelToken, Models[modelIndex], messages, response);
        var messageContent = response?.choices[0].message.content;
        while (string.IsNullOrEmpty(messageContent) && modelIndex < Models.Count - 1)
        {
            Log.Warning($"Model {Models[modelIndex].Model} doesn't work. Trying the next model: {Models[modelIndex + 1].Model}");
            ++modelIndex;
            response = await GetResponse(cancelToken, Models[modelIndex], messages, response);
        }

        if (string.IsNullOrEmpty(messageContent))
        {
            Log.Error("Can't use GPT4FREE :(");
            await GenerateResponse(client, update, cancelToken, myId);
            return;
        }
        Log.Information($"choices: {response?.choices.Count}");
        foreach (var gpt4FreeChoice in response!.choices)
        {
            Log.Information($"choice[{gpt4FreeChoice.index}]: {gpt4FreeChoice.message}");
        }

        var chatId = update.Message.Chat.Id;
        LogHistoryMessages(formattedMessage, messageContent, update.Message.Chat.Id);
        if (response is { model: not null, provider: not null })
        {
            using var responseConfigServiceScope = _scopeFactory.CreateScope();
            var responseConfigService = responseConfigServiceScope.ServiceProvider.GetRequiredService<ResponseConfigService>();
            var mode = responseConfigService.GetChatMode(chatId);
            LogAnalytics(chatId, response.model, response.provider, mode);
        }
        try
        {
            await client.SendMessage(
                chatId: chatId,
                text: messageContent,
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

    private async Task<GPT4FreeResponse?> GetResponse(CancellationToken cancelToken, ModelTimeout model, List<GPT4FreeMessage> messages,
        GPT4FreeResponse? response)
    {
        try
        {
            var chatRequest = new GPT4FreeRequest(model.Model, model.Provider, false, messages);
            var botClient = new HttpClient
            {
                BaseAddress = new Uri(_endpoint),
                Timeout = model.Timeout
            };
            var httpResponse =
                await botClient.PostAsJsonAsync("/v1/chat/completions", chatRequest, cancellationToken: cancelToken);
            if (httpResponse.StatusCode == HttpStatusCode.OK)
            {
                var jsonContent = await httpResponse.Content.ReadAsStringAsync(cancelToken);
                response = JsonSerializer.Deserialize<GPT4FreeResponse>(jsonContent);
            }
        }
        catch (TaskCanceledException)
        {
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception");
        }

        return response;
    }

    private List<GPT4FreeMessage> GetHistory(long chatId)
    {
        using var messageServiceScope = _scopeFactory.CreateScope();
        var messageService = messageServiceScope.ServiceProvider.GetRequiredService<MessageService>();
        return messageService.GetPreviousMessagesGpt4Free(chatId, 25);
    }
    
    private void LogHistoryMessages(string userMessage, string botResponse, long chatId)
    {
        using var messageServiceScope = _scopeFactory.CreateScope();
        var messageService = messageServiceScope.ServiceProvider.GetRequiredService<MessageService>();
        messageService.LogMessage(new HistoricalMessage { Content = userMessage, ChatId = chatId, IsBot = false });
        messageService.LogMessage(new HistoricalMessage { Content = botResponse, ChatId = chatId, IsBot = true });
    }
}