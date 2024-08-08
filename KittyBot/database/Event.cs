using System.ComponentModel.DataAnnotations;

namespace KittyBot.database;

public class Event
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int Day { get; set; }
    
    [Required]
    public int Month { get; set; }
    
    [Required]
    public string Title { get; set; }
    
    [Required]
    public string Description { get; set; }

    [Required]
    public bool Enabled { get; set; }
    
    [Required]
    public Locale Language { get; set; }
}