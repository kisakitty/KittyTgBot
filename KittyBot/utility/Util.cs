using System.Globalization;
using System.Text.RegularExpressions;
using KittyBot.database;
using Telegram.Bot;

namespace KittyBot;

public static class Util
{
    private const string CommandPattern = @"^/\w+(\s+\w+)*";
    private const string CommandWithArgsPattern = @"^/\w+(\s+\w+)*";

    public static bool IsCommand(string text)
    {
        return Regex.IsMatch(text, CommandPattern);
    }

    public static string FormatUserName(User user, bool mention)
    {
        if (user.Username is not null && !mention)
        {
            return $"[{user.Username.Replace("_", "\\_")}](https://t.me/{user.Username})";
        }

        if (user.Username is not null && mention)
        {
            return $"@{user.Username}";
        }

        return FormatNames(user);
    }

    public static string FormatUserName(Telegram.Bot.Types.User? user, bool mention)
    {
        if (user is null)
        {
            return "unknown";
        }
        if (user.Username is not null && !mention)
        {
            return $"[{user.Username.Replace("_", "\\_")}](https://t.me/{user.Username})";
        }

        if (user.Username is not null && mention)
        {
            return $"@{user.Username}";
        }

        return FormatNames(user);
    }

    public static string FormatNames(User user)
    {
        if (user.LastName is not null && user.LastName.Length > 0)
        {
            return $"{user.FirstName} {user.LastName}";
        }

        return user.FirstName;
    }

    public static string FormatNames(Telegram.Bot.Types.User? user)
    {
        if (user is null)
        {
            return "unknown";
        }
        if (user.LastName is not null && user.LastName.Length > 0)
        {
            return $"{user.FirstName} {user.LastName}";
        }

        return user.FirstName;
    }
    
    public static string LocalizeDate(DateTime dateTime, String langCulture) {

        CultureInfo culture = new CultureInfo(langCulture);
        return dateTime.ToString("m", culture);
    }

    public static string FormatMessage(Telegram.Bot.Types.Message tgMessage, long? myId)
    {
        var notMyMessage = myId == null || tgMessage.ReplyToMessage?.From?.Id != myId;
        var mainText = $"Никнейм: {FormatUserName(tgMessage.From, true)}; Имя: {FormatNames(tgMessage.From)}\nСообщение: [\n{tgMessage.Text ?? tgMessage.Caption}\n]";
        var replyText = tgMessage.ReplyToMessage?.Text ?? tgMessage.ReplyToMessage?.Caption;
        if (replyText != null && notMyMessage)
        {
            return $"{mainText}\n\nПересланное сообщение от {FormatUserName(tgMessage.ReplyToMessage?.From, true)}: [\n{replyText}\n]";
        }
        
        return mainText;
    }

    public static async Task<string?> GetPhotoBase64(ITelegramBotClient client, Telegram.Bot.Types.Message tgMessage, CancellationToken cancelToken)
    {
        if (tgMessage.Photo is null || tgMessage.Photo.Length == 0)
        {
            return null;
        }
        var photoId = tgMessage.Photo.Last().FileId;
        var fileInfo = await client.GetFile(photoId, cancellationToken: cancelToken);
        var filePath = fileInfo.FilePath;
        if (filePath is null)
        {
            return null;
        }
        var buffer = new byte[fileInfo.FileSize ?? 20 * 1024 * 1024];
        await using Stream fileStream = new MemoryStream(buffer);
        await client.DownloadFile(
            filePath: filePath,
            destination: fileStream,
            cancellationToken: cancelToken);
        return Convert.ToBase64String(buffer);
    }
}