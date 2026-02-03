using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetalFlowSystemV2.Data.Entities
{
    public class Item : IValidatableObject
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

        [Required]
        [MaxLength(3)]
        public string UOM { get; set; } = "PCS";

        [Column(TypeName = "decimal(18, 4)")]
        public decimal? PoundsPerSquareFoot { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (UOM == "PCS")
            {
                if (PoundsPerSquareFoot == null || PoundsPerSquareFoot <= 0)
                {
                    yield return new ValidationResult("PoundsPerSquareFoot is required and must be > 0 for Sheet items (PCS).", new[] { nameof(PoundsPerSquareFoot) });
                }
            }
            else if (UOM == "LBS")
            {
                if (PoundsPerSquareFoot != null && PoundsPerSquareFoot != 0)
                {
                    yield return new ValidationResult("PoundsPerSquareFoot must be 0 or empty for Coil items (LBS).", new[] { nameof(PoundsPerSquareFoot) });
                }
            }
        }
    }
}
