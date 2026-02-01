using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetalFlowSystemV2.Data.Entities
{
    public class InventoryStock
    {
        public int Id { get; set; }

        public int BranchId { get; set; }
        public Branch? Branch { get; set; }

        public int ItemId { get; set; }
        public Item? Item { get; set; }

        [Required]
        [MaxLength(50)]
        public string LocationCode { get; set; } = string.Empty;

        // Snapshot Values (Verbatim from import)
        public int? QuantityOnHand { get; set; } // For Sheet

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? WeightOnHand { get; set; } // For Coil

        public DateTime LastUpdatedAt { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
