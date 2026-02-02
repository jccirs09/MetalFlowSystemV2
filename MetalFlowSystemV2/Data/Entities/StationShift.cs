using System.ComponentModel.DataAnnotations;

namespace MetalFlowSystemV2.Data.Entities
{
    public class StationShift
    {
        public int Id { get; set; }

        public int BranchId { get; set; }
        public Branch? Branch { get; set; }

        public int PackingStationId { get; set; }
        public PackingStation? PackingStation { get; set; }

        public int ShiftTemplateId { get; set; }
        public Shift? ShiftTemplate { get; set; }

        public DateOnly ShiftDate { get; set; }

        public ShiftStatus Status { get; set; } = ShiftStatus.Planned;

        public int ExpectedHeadcount { get; set; }
        public int ConfirmedHeadcount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }

        public string? ClosedByUserId { get; set; }
        public ApplicationUser? ClosedByUser { get; set; }
    }
}
