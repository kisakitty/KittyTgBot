using System.ComponentModel.DataAnnotations;

namespace KittyBot.database;

public class ChatMessage
{
    [Key] public int Id { get; set; }

    [Required] public long ChatId { get; set; }

    [Required] public long MessageId { get; set; }

    [Required] public User Author { get; set; }
}