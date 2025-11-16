using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehiclePricePrediction.API.Models;

[Table("admin_permissions")]
public class AdminPermission
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("adminId", TypeName = "uniqueidentifier")]
    public Guid AdminId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("feature")]
    public string Feature { get; set; } = string.Empty;

    [Column("canAccess")]
    public bool CanAccess { get; set; } = true;

    [Column("canCreate")]
    public bool CanCreate { get; set; } = false;

    [Column("canEdit")]
    public bool CanEdit { get; set; } = false;

    [Column("canDelete")]
    public bool CanDelete { get; set; } = false;

    [ForeignKey("AdminId")]
    public User? Admin { get; set; }
}

