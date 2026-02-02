using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetalFlowSystemV2.Data.Entities
{
    public enum PickingListStatus
    {
        Queued,
        Picking,
        Packed,
        ReadyToShip,
        Loaded,
        Shipped,
        Cancelled
    }

    public class PickingList
    {
        [Key]
        public int Id { get; set; }

        public int BranchId { get; set; }

        [ForeignKey("BranchId")]
        public virtual Branch Branch { get; set; }

        [Required]
        [MaxLength(50)]
        public string PickingListNumber { get; set; }

        public DateTime? PrintDate { get; set; }
        public DateTime? ShipDate { get; set; }

        [MaxLength(100)]
        public string Buyer { get; set; }

        [MaxLength(100)]
        public string SalesRep { get; set; }

        [MaxLength(100)]
        public string ShipVia { get; set; }

        [MaxLength(200)]
        public string SoldTo { get; set; }

        [MaxLength(200)]
        public string ShipTo { get; set; }

        public string OrderInstructions { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalWeightLbs { get; set; }

        public PickingListStatus Status { get; set; } = PickingListStatus.Queued;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<PickingListLine> Lines { get; set; } = new List<PickingListLine>();
    }
}
