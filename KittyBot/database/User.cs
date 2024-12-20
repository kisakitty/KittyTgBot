using System.ComponentModel.DataAnnotations;

namespace KittyBot.database;

public class User
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public long UserId { get; set; }

    [Required]
    public bool IsAdmin { get; set;  }
    
    public string? Username { get; set; }
    
    [Required]
    public string FirstName { get; set; }
 
    public string? LastName { get; set; }
    
    
}