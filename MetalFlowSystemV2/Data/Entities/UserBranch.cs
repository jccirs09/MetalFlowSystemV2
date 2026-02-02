using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MetalFlowSystemV2.Data;

namespace MetalFlowSystemV2.Data.Entities
{
    public class UserBranch
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [Required]
        public int BranchId { get; set; }

        [ForeignKey("BranchId")]
        public Branch? Branch { get; set; }

        [Required]
        public string RoleId { get; set; } = string.Empty;

        public bool IsDefault { get; set; } = false;
    }
}
