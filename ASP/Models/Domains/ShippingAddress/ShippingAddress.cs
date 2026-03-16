using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ASP.Models.Admin.Accounts;

namespace ASP.Models.Domains
{
    public class ShippingAddress
    {
        [Key]
        public int AddressId { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string FullName { get; set; }

        [Required]
        [StringLength(20)]
        public string Phone { get; set; }

        [Required]
        [StringLength(300)]
        public string AddressLine { get; set; }

        public string City { get; set; }

        public string District { get; set; }

        public string Ward { get; set; }

        public bool IsDefault { get; set; } = false;

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }
    }
}