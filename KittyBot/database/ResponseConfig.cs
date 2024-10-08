using System.ComponentModel.DataAnnotations;

namespace KittyBot.database;

public class ResponseConfig
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public long ChatId { get; set; }

    [Required]
    public bool HelloMessage { get; set; }
    
    [Required]
    public bool ChatBot { get; set; }
}
