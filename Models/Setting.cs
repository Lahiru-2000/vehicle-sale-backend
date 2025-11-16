using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehiclePricePrediction.API.Models;

[Table("settings")]
public class Setting
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("settingKey")]
    public string SettingKey { get; set; } = string.Empty;

    [Column("value", TypeName = "nvarchar(MAX)")]
    public string? Value { get; set; }

    [Column("updatedAt")]
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
}

