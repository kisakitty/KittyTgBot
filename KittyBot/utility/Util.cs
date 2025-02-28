using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using KittyBot.dto;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = KittyBot.database.User;

namespace KittyBot.utility;

public static partial class Util
{
    private const string CommandPattern = @"^/\w+(\s+\w+)*";
    private const string CommandWithArgsPattern = @"^/\w+(\s+\w+)*";

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

    public static async Task<List<MessageContent>> FormatMessageWithReplies(ITelegramBotClient client,
        Message tgMessage, long? myId, CancellationToken cancelToken)
    {
        string? mainText = null;
        var textContent = tgMessage.Text ?? tgMessage.Caption;
        if (tgMessage.From?.Id != myId && textContent is not null)
            mainText = tgMessage.From?.Username is not null
                ? $"Никнейм: {FormatUserName(tgMessage.From, true)}; Имя: {FormatNames(tgMessage.From)}\nСообщение: [\n{textContent}\n]"
                : $"Имя: {FormatNames(tgMessage.From)}\nСообщение: [\n{textContent}\n]";
        var photo = await GetPhotoBase64(client, tgMessage, cancelToken);
        var replies = tgMessage.ReplyToMessage != null
            ? await FormatMessageWithReplies(client, tgMessage.ReplyToMessage, myId, cancelToken)
            : null;
        if (textContent is null && photo is null) return replies ?? [];
        var thisMessageContent = new MessageContent(mainText, photo);
        return replies is null ? [thisMessageContent] : replies.Append(thisMessageContent).ToList();
    }


    private static async Task<string?> GetPhotoBase64(ITelegramBotClient client, Message tgMessage,
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

    [GeneratedRegex(CommandPattern)]
    private static partial Regex MyRegex();
}