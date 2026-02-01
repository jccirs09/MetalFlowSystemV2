using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetalFlowSystemV2.Data.Entities
{
    public class InventoryStock
    {
        public int Id { get; set; }

        public int BranchId { get; set; }

        [ForeignKey("BranchId")]
        public Branch? Branch { get; set; }

        public int ItemId { get; set; }

        [ForeignKey("ItemId")]
        public Item? Item { get; set; }

        [MaxLength(50)]
        public string? LocationCode { get; set; }

        [Column(TypeName = "decimal(18, 3)")]
        public decimal QuantityOnHand { get; set; } = 0;

        [Column(TypeName = "decimal(18, 3)")]
        public decimal? WeightOnHand { get; set; }

        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;
    }
}
