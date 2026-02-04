using System;

namespace MetalFlowSystemV2.Client.Models
{
    public class UserAssignmentDto
    {
        public string UserId { get; set; } = string.Empty;
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;

        public string WorkMode { get; set; } = "None"; // "ProductionArea" or "PackingStation"

        public int? ProductionAreaId { get; set; }
        public string? ProductionAreaName { get; set; }

        public int? PackingStationId { get; set; }
        public string? PackingStationName { get; set; }

        public int ShiftTemplateId { get; set; }
        public string ShiftName { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public bool IsCheckedIn { get; set; }
        public DateTime? CheckedInAt { get; set; }
    }
}
