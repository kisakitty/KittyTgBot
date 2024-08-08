using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KittyBot.database;
using KittyBot.dto.gemini;
using KittyBot.exceptions;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace KittyBot.handlers;

public class HelloHandler: Handler
{
    private readonly GeminiConfig _generationConfig = new (0.9f, 2000);
    
    private readonly List<GeminiSafetyParameter> _safetyParameter = new()
    {
        new GeminiSafetyParameter("HARM_CATEGORY_HARASSMENT", "BLOCK_NONE"),
        new GeminiSafetyParameter("HARM_CATEGORY_HATE_SPEECH", "BLOCK_NONE"),
        new GeminiSafetyParameter("HARM_CATEGORY_SEXUALLY_EXPLICIT", "BLOCK_NONE"),
        new GeminiSafetyParameter("HARM_CATEGORY_DANGEROUS_CONTENT", "BLOCK_NONE")
    };
    
    private static readonly HttpClient BotClient = new()
    {
        BaseAddress = new Uri("https://generativelanguage.googleapis.com"),
        Timeout = new TimeSpan(0, 1, 0)
    };
    
    public override async Task HandleUpdate(ITelegramBotClient client, Update update, CancellationToken cancelToken, Locale language = Locale.RU)
    {
        if (update.Message?.From is null) return;
        var response = await GetResponse(Util.FormatUserName(update.Message.From, true), "gemini-pro", cancelToken);
        await client.SendTextMessageAsync(
            chatId: update.Message.Chat.Id,
            text: response,
            cancellationToken: cancelToken,
            linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true },
            replyParameters: new ReplyParameters { ChatId = update.Message.Chat.Id }
        );
    }
    
    private async Task<string?> GetResponse(string? user, string model, CancellationToken cancelToken)
    {
        string? jsonContent = null;
        var _apikey = Environment.GetEnvironmentVariable(GeminiHandler.GeminiApiKeyEnv) ??
                      throw new EnvVariablesException($"Expect Gemini API key. Set it to environment variable {GeminiHandler.GeminiApiKeyEnv}");
        try
        {
            var contents = new List<GeminiMessage>
            {
                new(new List<GeminiContent>
                    {
                        new(
                            "Ты бот в небольшом чате, основная твоя задача — приветствовать новых пользователей. Придерживайся неформального и молодёжного стиля, будь максимально оригинальным и непредсказуемым, шути, пугай и т. д.. Я напишу тебе лишь никнейм пользователя. Не пиши ничего кроме приветствия! Расскажи что ты умеешь поздравлять с днём рождения (нужно вбить свой день рождения при помощи команды /setbirthday), отдавать фоточки котиков по команде /cat и что ты очень умный бот, придумай комплимент пользователю! Длина приветствия от 400 до 1000 символов. Не используй Markdown!",
                            null
                        )
                    },
                    "user"),
                new(new List<GeminiContent>
                    {
                        new($"Пользователь: {user}",
                            null
                        )
                    },
                    "user"),
            };
            var chatRequest = new GeminiRequest(contents, null, _generationConfig, _safetyParameter);
            var httpResponse =
                await BotClient.PostAsJsonAsync($"/v1beta/models/{model}:generateContent?key={_apikey}", chatRequest, cancellationToken: cancelToken);
            if (httpResponse.StatusCode == HttpStatusCode.OK)
            {
                jsonContent = await httpResponse.Content.ReadAsStringAsync(cancelToken);
                var response = JsonSerializer.Deserialize<GeminiResponse>(jsonContent);
                var result = response?.candidates?[0].content.parts[0].text;
                if (result is null)
                {
                    throw new GeminiException(jsonContent ?? "");
                }
                return result;
            }
        }
        catch (TaskCanceledException)
        {
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception");
            if (jsonContent is not null)
            {
                Log.Error(jsonContent);
            }
        }

        return null;

    }
}