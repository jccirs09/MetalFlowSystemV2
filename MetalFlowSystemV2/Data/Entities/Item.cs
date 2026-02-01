using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetalFlowSystemV2.Data.Entities
{
    public class Item
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ItemCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;

        public ItemType Type { get; set; }

        // Hierarchy
        public int? ParentItemId { get; set; }
        public Item? ParentItem { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
