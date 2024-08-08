using System.ComponentModel.DataAnnotations;

namespace KittyBot.database;

public class Stats
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public long ChatId { get; set; }
    
    [Required]
    public User User { get; set; }

    [Required]
    public long CountMessages { get; set; }
}