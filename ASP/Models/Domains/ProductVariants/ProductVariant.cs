using ASP.Models.Domains;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASP.Models.Domains
{
    public class ProductVariant
    {
        [Key]
        public int VariantId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [StringLength(50)]
        public string SKU { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public bool IsActive { get; set; } = true;

        [Range(0, int.MaxValue)]
        public int QuantityVariants { get; set; } = 0;

        [StringLength(20)]
        public string? Size { get; set; }

        [StringLength(50)]
        public string? Color { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; }
    }
}
