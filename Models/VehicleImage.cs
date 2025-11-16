using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehiclePricePrediction.API.Models;

[Table("vehicle_images")]
public class VehicleImage
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("vehicleId")]
    public int VehicleId { get; set; }

    [Required]
    [Column("imageData", TypeName = "nvarchar(MAX)")]
    public string ImageData { get; set; } = string.Empty;

    [MaxLength(255)]
    [Column("fileName")]
    public string? FileName { get; set; }

    [MaxLength(100)]
    [Column("mimeType")]
    public string? MimeType { get; set; }

    [Column("fileSize")]
    public long? FileSize { get; set; }

    [Column("sortOrder")]
    public int SortOrder { get; set; } = 0;

    [Column("uploadedAt")]
    public DateTime? UploadedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("VehicleId")]
    public Vehicle? Vehicle { get; set; }
}

