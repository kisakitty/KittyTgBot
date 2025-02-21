using System.Text;

namespace KittyBot.utility;

public static class StringUtils
{
    public static List<string> SplitTextIntoChunks(string text, int chunkSize)
    {
        var sb = new StringBuilder();
        var chunks = new List<string>();

        foreach (var line in text.Split([Environment.NewLine], StringSplitOptions.None))
        {
            if (sb.Length + line.Length > chunkSize)
            {
                chunks.Add(sb.ToString());
                sb.Length = 0;
            }

            sb.Append($"{line}\n");
        }

        chunks.Add(sb.ToString());
        return chunks;
    }
}