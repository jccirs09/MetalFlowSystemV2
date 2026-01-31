using System.ComponentModel.DataAnnotations;

namespace MetalFlowSystemV2.Data.Entities;

public class Shift
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

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public bool CrossesMidnight { get; set; }

    public bool IsActive { get; set; } = true;
}
