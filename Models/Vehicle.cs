using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehiclePricePrediction.API.Models;

[Table("Vehicles")]
public class Vehicle
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("brand")]
    public string Brand { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("model")]
    public string Model { get; set; } = string.Empty;

    [Required]
    [Column("year")]
    public int Year { get; set; }

    [Required]
    [Column("price", TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("type")]
    public string Type { get; set; } = "car";

    [Required]
    [MaxLength(50)]
    [Column("fuelType")]
    public string FuelType { get; set; } = "petrol";

    [Required]
    [MaxLength(50)]
    [Column("transmission")]
    public string Transmission { get; set; } = "manual";

    [Required]
    [MaxLength(10)]
    [Column("condition")]
    public string Condition { get; set; } = "USED";

    [Required]
    [Column("mileage")]
    public int Mileage { get; set; }

    [Required]
    [Column("description", TypeName = "nvarchar(MAX)")]
    public string Description { get; set; } = string.Empty;

    [Column("images", TypeName = "nvarchar(MAX)")]
    public string Images { get; set; } = "[]";

    [Required]
    [Column("contactInfo", TypeName = "nvarchar(MAX)")]
    public string ContactInfo { get; set; } = string.Empty;

    [MaxLength(50)]
    [Column("status")]
    public string Status { get; set; } = "pending";

    [Required]
    [Column("userId", TypeName = "uniqueidentifier")]
    public Guid UserId { get; set; }

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [Column("approvedAt")]
    public DateTime? ApprovedAt { get; set; }

    [Column("isPremium")]
    public bool IsPremium { get; set; } = false;

    [ForeignKey("UserId")]
    public User? User { get; set; }
}

