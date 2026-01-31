using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MetalFlowSystemV2.Data;

namespace MetalFlowSystemV2.Data.Entities;

public class Truck
{
    public int Id { get; set; }

    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    [Required]
    [MaxLength(20)]
    public string TruckCode { get; set; } = string.Empty;

    [Column(TypeName = "decimal(12, 2)")]
    public decimal CapacityLbs { get; set; }

    [Required]
    [MaxLength(20)]
    public string Plate { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public string? AssignedDriverUserId { get; set; }

    [ForeignKey("AssignedDriverUserId")]
    public ApplicationUser? AssignedDriver { get; set; }

    public bool IsActive { get; set; } = true;
}
