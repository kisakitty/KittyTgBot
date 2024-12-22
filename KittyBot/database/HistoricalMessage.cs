using System.ComponentModel.DataAnnotations;

namespace KittyBot.database;

public class HistoricalMessage
{
    [Key] public int Id { get; set; }

    [Required] public string Content { get; set; }

    [Required] public long ChatId { get; set; }

    [Required] public bool IsBot { get; set; }

    [Required] public ChatMode ChatMode { get; set; }
}