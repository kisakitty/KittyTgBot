using System.ComponentModel.DataAnnotations;

namespace KittyBot.database;

public class Reaction
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public long ChatId { get; set; }
    
    [Required]
    public User User { get; set; }

    [Required]
    public long Count { get; set; }
    
    [Required]
    public String Emoji { get; set; }
}