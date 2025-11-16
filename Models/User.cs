using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehiclePricePrediction.API.Models;

[Table("users")]
public class User
{
    [Key]
    [Column("id", TypeName = "uniqueidentifier")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(255)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("password")]
    public string Password { get; set; } = string.Empty;

    [MaxLength(50)]
    [Column("role")]
    public string Role { get; set; } = "user";

    [Column("isBlocked")]
    public bool IsBlocked { get; set; } = false;

    [MaxLength(20)]
    [Column("phone")]
    public string? Phone { get; set; }

    [Column("lastLogin")]
    public DateTime? LastLogin { get; set; }

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

