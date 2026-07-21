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

        [Required(ErrorMessage = "Vui lòng nhập Họ Tên")]
        [StringLength(200)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Số Điện Thoại")]
        [StringLength(20)]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Địa chỉ chi tiết")]
        [StringLength(300)]
        public string AddressLine { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Tỉnh/Thành phố")]
        public string City { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Quận/Huyện")]
        public string District { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Phường/Xã")]
        public string Ward { get; set; }

        public bool IsDefault { get; set; } = false;

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }
    }
}