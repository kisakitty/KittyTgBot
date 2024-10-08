using KittyBot.database;
using KittyBot.dto.gemini;
using KittyBot.exceptions;
using KittyBot.services;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace KittyBot.handlers;

public class HelloHandler(long? botId) : Handler
{
    private readonly GeminiBot _geminiBot = new();

    public override async Task HandleUpdate(ITelegramBotClient client, Update update, CancellationToken cancelToken, Locale language = Locale.RU)
    {
        if (update.Message?.NewChatMembers is null) return;
        foreach (var newMember in update.Message.NewChatMembers)
        {
            var chat = await client.GetChatAsync(update.Message!.Chat.Id, cancelToken);
            var userName = Util.FormatUserName(newMember, true);
            var fullName = Util.FormatNames(newMember);
            var chatTitle = update.Message?.Chat.Title;
            var response = botId != newMember.Id ?
                await GetResponse(userName, fullName, chatTitle, chat.Description, cancelToken) : 
                await GenerateBotIntroduction(userName, fullName, chatTitle, chat.Description, cancelToken);
            if (response == null)
            {
                continue;
            }
            await client.SendTextMessageAsync(
                chatId: update.Message!.Chat.Id,
                text: response,
                cancellationToken: cancelToken,
                linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true }
            );
            Thread.Sleep(500);
        }
    }

    private async Task<string?> GetResponse(string? userName, string? fullName, string? chatTitle,
        string? chatDescription, CancellationToken cancelToken)
    {
        try
        {
            var contents = new List<GeminiMessage>
            {
                new(new List<GeminiContent>
                    {
                        new(
                            "Ты бот в небольшом чате, основная твоя задача — приветствовать новых пользователей. " +
                            "Придерживайся неформального и молодёжного стиля, будь максимально оригинальным и непредсказуемым, шути, пугай и т. д. " +
                            "Я напишу тебе лишь никнейм пользователя и его имя. Не пиши ничего кроме приветствия! " +
                            "Расскажи что ты умеешь поздравлять с днём рождения (нужно вбить свой день рождения при помощи команды в формате /setbirthday DD-MM), " +
                            "отдавать фоточки котиков по команде /cat и что ты очень умный бот, придумай комплимент пользователю! Также расскажи про навшу беседу, я дам тебе название с описанием!" +
                            "Длина приветствия от 400 до 600 символов. НИ В КОЕМ СЛУЧАЕ НЕ ИСПОЛЬЗУЙ MARKDOWN!",
                            null
                        )
                    },
                    "user"),
                new(new List<GeminiContent>
                    {
                        new($"Никнейм нового пользователя: {userName}. Имя нового пользователя: {fullName}. Беседа: {chatTitle ?? "без названия"}. Описание беседы: {chatDescription ?? "без описания"}",
                            null
                        )
                    },
                    "user"),
            };
            return await _geminiBot.GenerateTextResponse(contents, "gemini-pro", cancelToken);
        }
        catch (GeminiException ex)
        {
            Log.Error(ex, "Gemini API error");
        }

        return null;
    }

    private async Task<string?> GenerateBotIntroduction(string? botName, string? botFullName, string? chatTitle,
        string? chatDescription, CancellationToken cancelToken)
    {
        try
        {
            var contents = new List<GeminiMessage>
            {
                new(new List<GeminiContent>
                    {
                        new(
                            "Ты бот в небольшом чате, основная твоя задача сейчас — представить себя перед другими пользователями! " +
                            "Придерживайся неформального и молодёжного стиля, будь максимально оригинальным и непредсказуемым, шути, пугай и т. д. " +
                            "Я напишу тебе твой никнейм и твоё имя. Не пиши ничего кроме своего описания! " +
                            "Расскажи что ты умеешь поздравлять с днём рождения (нужно вбить свой день рождения при помощи команды в формате /setbirthday DD-MM), " +
                            "отдавать фоточки котиков по команде /cat и что ты очень умный бот! Также расскажи почему ты рад присоединиться к беседе, я дам тебе название с описанием!" +
                            "Длина приветствия от 400 до 1000 символов. Не используй Markdown!",
                            null
                        )
                    },
                    "user"),
                new(new List<GeminiContent>
                    {
                        new($"Твой никнейм: {botName}. Твоё имя: {botFullName}. " +
                            $"Беседа: {chatTitle ?? "без названия"}. Описание беседы: {chatDescription ?? "без описания"}",
                            null
                        )
                    },
                    "user"),
            };
            return await _geminiBot.GenerateTextResponse(contents, "gemini-pro", cancelToken);
        }
        catch (GeminiException ex)
        {
            Log.Error(ex, "Gemini API error");
        }

        return null;
    }
}