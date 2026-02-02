using System.ComponentModel.DataAnnotations;

namespace MetalFlowSystemV2.Data.Entities
{
    public class PackingStation
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public int BranchId { get; set; }
        public Branch? Branch { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
