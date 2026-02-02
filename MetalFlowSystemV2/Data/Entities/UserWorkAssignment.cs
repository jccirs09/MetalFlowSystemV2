using System.ComponentModel.DataAnnotations;

namespace MetalFlowSystemV2.Data.Entities
{
    public enum WorkMode
    {
        ProductionArea = 1,
        PackingStation = 2
    }

    public enum UserRole
    {
        Operator = 1,
        Supervisor = 2,
        Planner = 3
    }

    public class UserWorkAssignment
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        public int BranchId { get; set; }
        public Branch? Branch { get; set; }

        public int ShiftTemplateId { get; set; }
        public Shift? ShiftTemplate { get; set; }

        public WorkMode WorkMode { get; set; }

        public int? ProductionAreaId { get; set; }
        public ProductionArea? ProductionArea { get; set; }

        public int? PackingStationId { get; set; }
        public PackingStation? PackingStation { get; set; }

        public UserRole Role { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
