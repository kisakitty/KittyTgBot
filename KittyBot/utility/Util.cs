using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = KittyBot.database.User;

namespace KittyBot.utility;

public static partial class Util
{
    private const string CommandPattern = @"^/\w+(\s+\w+)*";
    private const string CommandWithArgsPattern = @"^/\w+(\s+\w+)*";
    public const int MaxChunkSize = 4096;

    private static readonly IList<char> SpecialChars =
        new ReadOnlyCollection<char>(new List<char>
            { '_', '*', '[', ']', '(', ')', '~', '`', '>', '#', '+', '-', '=', '|', '{', '}', '.', '!' });
    
    public static bool IsCommand(string text)
    {
        return MyRegex().IsMatch(text);
    }

    // FIXME use FormatUserName(User user, bool mention)
    public static string FormatUserName(Telegram.Bot.Types.User? user, bool mention)
    {
        if (user is null) return "unknown";
        if (user.Username is not null && !mention)
            return $"[{user.Username.Replace("_", "\\_")}](https://t.me/{user.Username})";

        if (user.Username is not null && mention) return $"@{user.Username}";

        return FormatNames(user);
    }

    public static string FormatNames(Telegram.Bot.Types.User? user)
    {
        if (user is null) return "unknown";
        if (user.LastName is not null && user.LastName.Length > 0)
            return EscapeSpecialSymbols($"{user.FirstName} {user.LastName}");

        return EscapeSpecialSymbols(user.FirstName);
    }

    public static string FormatUserName(User user, bool mention, bool userIdLink)
    {
        if (user.Username is not null && !mention)
            return $"[{user.Username.Replace("_", "\\_")}](https://t.me/{user.Username})";

        if (user.Username is not null && mention) return $"@{user.Username}";

        return FormatNames(user, userIdLink);
    }

    public static string FormatNames(User user, bool userIdLink)
    {
        var escapedFirstLastNames = EscapeSpecialSymbols($"{user.FirstName} {user.LastName}");
        if (user.LastName is not null && user.LastName.Length > 0 && !userIdLink)
            return escapedFirstLastNames;
        if (user.LastName is not null && user.LastName.Length > 0 && userIdLink)
            return $"[{escapedFirstLastNames}](tg://user?id={user.UserId})";

        var escapedFirstName = EscapeSpecialSymbols(user.FirstName);
        return !userIdLink ? escapedFirstName : $"[{escapedFirstName}](tg://user?id={user.UserId})";
    }

    public static string LocalizeDate(DateTime dateTime, string langCulture)
    {
        var culture = new CultureInfo(langCulture);
        return dateTime.ToString("m", culture);
    }

    public static string FormatMessage(Message tgMessage, long? myId)
    {
        var notMyMessage = myId == null || tgMessage.ReplyToMessage?.From?.Id != myId;
        var mainText =
            $"Никнейм: {FormatUserName(tgMessage.From, true)}; Имя: {FormatNames(tgMessage.From)}\nСообщение: [\n{tgMessage.Text ?? tgMessage.Caption}\n]";
        var replyText = tgMessage.ReplyToMessage?.Text ?? tgMessage.ReplyToMessage?.Caption;
        if (replyText != null && notMyMessage)
            return
                $"{mainText}\n\nПересланное сообщение от {FormatUserName(tgMessage.ReplyToMessage?.From, true)}: [\n{replyText}\n]";

        return mainText;
    }

    public static async Task<string?> GetPhotoBase64(ITelegramBotClient client, Message tgMessage,
        CancellationToken cancelToken)
    {
        if (tgMessage.Photo is null || tgMessage.Photo.Length == 0) return null;
        var photoId = tgMessage.Photo.Last().FileId;
        var fileInfo = await client.GetFile(photoId, cancelToken);
        var filePath = fileInfo.FilePath;
        if (filePath is null) return null;
        var buffer = new byte[fileInfo.FileSize ?? 20 * 1024 * 1024];
        await using Stream fileStream = new MemoryStream(buffer);
        await client.DownloadFile(
            filePath,
            fileStream,
            cancelToken);
        return Convert.ToBase64String(buffer);
    }

    public static string EscapeSpecialSymbols(string? text, string[]? exceptions = null)
    {
        if (text == null) return "";
        text = SpecialChars.Aggregate(text, (current, ch) => current.Replace(ch.ToString(), "\\" + ch));
        exceptions?
            .Select((e, index) => new { value = EscapeSpecialSymbols(e), i = index })
            .ToList()
            .ForEach(escapedException => text = text.Replace(escapedException.value, exceptions[escapedException.i]));
        return text;
    }

    public static bool ContentIsEmpty(string? text)
    {
        if (text == null) return true;
        text = SpecialChars.Aggregate(text, (current, ch) => current.Replace(ch.ToString(), ""));
        return text.Trim().Length == 0;
    }

    public static List<string> SplitIntoChunks(string text, int limit = MaxChunkSize)
    {
        var chunks = new List<string>();
        var sb = new StringBuilder();
        foreach (var line in text.Split("\n"))
        {
            if (sb.Length + line.Length + 1 > limit)
            {
                chunks.Add(sb.ToString());
                sb.Length = 0;
            }
            sb.Append(line + "\n");
        }
        chunks.Add(sb.ToString());
        return chunks;
    }

    [GeneratedRegex(CommandPattern)]
    private static partial Regex MyRegex();
}