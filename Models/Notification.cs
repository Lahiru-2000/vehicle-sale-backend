using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehiclePricePrediction.API.Models;

[Table("notifications")]
public class Notification
{
    [Key]
    [Column("id")]
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [Column("userId", TypeName = "uniqueidentifier")]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("type")]
    public string Type { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Column("message", TypeName = "nvarchar(MAX)")]
    public string Message { get; set; } = string.Empty;

    [Column("isRead")]
    public bool IsRead { get; set; } = false;

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    [Column("relatedEntityType")]
    public string? RelatedEntityType { get; set; }

    [MaxLength(36)]
    [Column("relatedEntityId")]
    public string? RelatedEntityId { get; set; }

    [ForeignKey("UserId")]
    public User? User { get; set; }
}

