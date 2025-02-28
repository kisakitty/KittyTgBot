using System.Text;

namespace KittyBot.utility;

public static class StringUtils
{
    private const int MaxChunkSize = 4096;

    public static List<string> SplitTextIntoChunks(string text, int chunkSize = MaxChunkSize)
    {
        var sb = new StringBuilder();
        var chunks = new List<string>();

        foreach (var line in text.Split([Environment.NewLine], StringSplitOptions.None))
        {
            if (sb.Length > 0 && sb.Length + line.Length > chunkSize)
            {
                chunks.Add(sb.ToString());
                sb.Length = 0;
            }

            sb.Append($"{line}\n");
        }

        if (sb.Length > 0) chunks.Add(sb.ToString());

        return chunks;
    }
}