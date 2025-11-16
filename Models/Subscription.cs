using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehiclePricePrediction.API.Models;

[Table("subscriptions")]
public class Subscription
{
    [Key]
    [Column("id")]
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [Column("userId", TypeName = "uniqueidentifier")]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("planType")]
    public string PlanType { get; set; } = string.Empty;

    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "active";

    [Required]
    [Column("startDate")]
    public DateTime StartDate { get; set; }

    [Required]
    [Column("endDate")]
    public DateTime EndDate { get; set; }

    [Required]
    [Column("price", TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [MaxLength(50)]
    [Column("paymentMethod")]
    public string? PaymentMethod { get; set; }

    [MaxLength(255)]
    [Column("transactionId")]
    public string? TransactionId { get; set; }

    [Column("postCount")]
    public int PostCount { get; set; } = 0;

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("cancelledAt")]
    public DateTime? CancelledAt { get; set; }

    [ForeignKey("UserId")]
    public User? User { get; set; }
}

