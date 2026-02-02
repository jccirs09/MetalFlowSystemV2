using System.ComponentModel.DataAnnotations;

namespace MetalFlowSystemV2.Data.Entities
{
    public enum ShiftStatus
    {
        Planned = 0,
        Active = 1,
        Closed = 2
    }

    public class AreaShift
    {
        public int Id { get; set; }

        public int BranchId { get; set; }
        public Branch? Branch { get; set; }

        public int ProductionAreaId { get; set; }
        public ProductionArea? ProductionArea { get; set; }

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
