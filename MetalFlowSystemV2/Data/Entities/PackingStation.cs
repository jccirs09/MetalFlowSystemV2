using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetalFlowSystemV2.Data.Entities
{
    public class PackingStation
    {
        public int Id { get; set; }

        public int BranchId { get; set; }

        [ForeignKey("BranchId")]
        public Branch? Branch { get; set; }

        [Required]
        [MaxLength(100)]
        public string StationName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? StationCode { get; set; }

        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;
    }
}
