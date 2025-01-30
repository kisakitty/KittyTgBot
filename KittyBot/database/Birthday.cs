using System.ComponentModel.DataAnnotations;

namespace KittyBot.database;

public class Birthday
{
    [Key] public int Id { get; set; }

    [Required] public int Day { get; set; }

    [Required] public int Month { get; set; }

    [Required] public User User { get; set; }
}