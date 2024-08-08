using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace KittyBot.database;

public class ModelAnalytic
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public long ChatId { get; set; }

    [Required]
    [NotNull]
    public string? Model { get; set; }
    
    [Required]
    [NotNull]
    public string? Provider { get; set; }
    
    [Required]
    [NotNull]
    public long? CountRequests { get; set; }
}