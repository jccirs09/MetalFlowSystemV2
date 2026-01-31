using System.ComponentModel.DataAnnotations;

namespace MetalFlowSystemV2.Data.Entities;

public enum ProductionAreaType
{
    CTL,
    Slitter,
    SheetPicking,
    CoilPicking,
    Packing
}

public class ProductionArea
{
    public int Id { get; set; }

    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public ProductionAreaType AreaType { get; set; }

    public bool IsActive { get; set; } = true;
}
