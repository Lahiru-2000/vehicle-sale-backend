using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehiclePricePrediction.API.Models;

[Table("favorites")]
public class Favorite
{
    [Key]
    [Column("id")]
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [Column("userId", TypeName = "uniqueidentifier")]
    public Guid UserId { get; set; }

    [Required]
    [Column("vehicleId")]
    public int VehicleId { get; set; }

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("UserId")]
    public User? User { get; set; }

    [ForeignKey("VehicleId")]
    public Vehicle? Vehicle { get; set; }
}

