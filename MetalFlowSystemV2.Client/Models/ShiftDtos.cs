namespace MetalFlowSystemV2.Client.Models
{
    public class UserAssignmentDto
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string ShiftName { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string WorkMode { get; set; } = string.Empty; // "Production" or "Packing"
        public string LocationName { get; set; } = string.Empty;
        public bool IsCheckedIn { get; set; }
        public DateTime? CheckedInAt { get; set; }
        public string? Message { get; set; }
    }

    public class CheckInRequest
    {
        public int BranchId { get; set; }
    }

    public class CheckInResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
