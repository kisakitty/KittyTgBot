using System.ComponentModel.DataAnnotations;

namespace KittyBot.database;

public class ChatLanguage
{
    [Key]
    public int Id { get; set; }

    [Required]
    public long ChatId;
    
    [Required]
    public Locale Language { get; set;  }
}