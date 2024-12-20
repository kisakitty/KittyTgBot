using KittyBot.database;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace KittyBot.handlers;

public class ReactionHandler: Handler
{
    private static readonly Dictionary<string, string> KeywordsReactionsSubstring = new()
    {
        { "китти", "❤" }, { "hello kitty", "❤" },
        { "миздец от пиходы", "💊" },
        { "аллах", "🙏" }, { "коран", "🙏" },
        { "шпэк", "👍" }, { "шпек", "👍" },
        { " вкид", "🎉" }, { "вкид ", "🎉" },
        { "1984", "👀" },
        { "/gayporn", "🍓" }, { "/gayporn@kisakittybot", "🍓" },
        { "черножоп", "🌚" },
        { "вход в пустоту", "❤️‍🔥" }, { "повелитель мух", "❤️‍🔥" }, { "завтрак на плутоне", "❤️‍🔥" },
        { "укрась прощальное утро цветами обещания", "❤️‍🔥" }, { "девочка покорившая время", "❤️‍🔥" }, 
        { "девочка, покорившая время", "❤️‍🔥" }, { "джонни взял ружье", "❤️‍🔥" }
    };

    public override async Task HandleUpdate(ITelegramBotClient client, Update update, CancellationToken cancelToken, Locale language = Locale.RU)
    {
        if (update.Message?.Text == null) return;
        
        foreach (var keyValue in KeywordsReactionsSubstring)
        {
            if (update.Message.Text.ToLower().Contains(keyValue.Key))
            {
                await client.SetMessageReaction(
                    new ChatId(update.Message.Chat.Id),
                    update.Message.MessageId,
                    new List<ReactionType> { keyValue.Value },
                    false,
                    cancelToken
                );
                return;
            }
        }
    }
}