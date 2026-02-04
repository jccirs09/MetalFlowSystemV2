using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetalFlowSystemV2.Data.Entities
{
    public class PackingEvent
    {
        [Key]
        public int Id { get; set; }

        public int StationShiftId { get; set; }
        [ForeignKey("StationShiftId")]
        public virtual StationShift StationShift { get; set; }

        public int PickingListId { get; set; }
        [ForeignKey("PickingListId")]
        public virtual PickingList PickingList { get; set; }

        public DateTime PackedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PackedWeightLbs { get; set; }

        // JSON blob or similar to store which lines were packed in this event if doing partials?
        // Or simple aggregate for now as per requirements.
        // "LinesPacked (aggregate or per-line)"
        // Let's store a simple count or serialized ID list for now if needed,
        // or rely on PickingListLine status updates.
        // Requirement says "LinesPacked (aggregate or per-line)".
        // I'll add a simple count.
        public int LineCount { get; set; }
    }
}
