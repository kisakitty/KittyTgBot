using System.ComponentModel.DataAnnotations;

namespace KittyBot.database;

public class ChatLanguage
{
    [Required] public long ChatId;

    [Key] public int Id { get; set; }

    [Required] public Locale Language { get; set; }
}