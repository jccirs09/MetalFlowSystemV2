using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MetalFlowSystemV2.Data.Entities.Enums;

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

        public ItemType ItemType { get; set; }

        public int? ParentItemId { get; set; }

        [ForeignKey("ParentItemId")]
        public Item? ParentItem { get; set; }

        public ICollection<Item> ChildItems { get; set; } = new List<Item>();

        [MaxLength(20)]
        public string? Uom { get; set; }

        [MaxLength(50)]
        public string? Category { get; set; }

        [Column(TypeName = "decimal(12, 3)")]
        public decimal? WeightPerUnit { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
