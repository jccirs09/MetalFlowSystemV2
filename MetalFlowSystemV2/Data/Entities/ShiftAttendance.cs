using System.ComponentModel.DataAnnotations;

namespace MetalFlowSystemV2.Data.Entities
{
    public class ShiftAttendance
    {
        public int Id { get; set; }

        public int BranchId { get; set; }
        public Branch? Branch { get; set; }

        public DateOnly ShiftDate { get; set; }

        public int ShiftTemplateId { get; set; }
        public Shift? ShiftTemplate { get; set; }

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        public WorkMode WorkMode { get; set; }

        public int? ProductionAreaId { get; set; }
        public ProductionArea? ProductionArea { get; set; }

        public int? PackingStationId { get; set; }
        public PackingStation? PackingStation { get; set; }

        public int? AreaShiftId { get; set; }
        public AreaShift? AreaShift { get; set; }

        public int? StationShiftId { get; set; }
        public StationShift? StationShift { get; set; }

        public DateTime CheckedInAt { get; set; } = DateTime.UtcNow;

        public bool HeadcountOverride { get; set; } = false;

        [MaxLength(200)]
        public string? OverrideReason { get; set; }
    }
}
