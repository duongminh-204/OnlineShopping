using ASP.Models.ASPModel;
using ASP.Models.Domains;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace ASP.Controllers.Front
{
    [Route("api/address")]
    [ApiController]
    [Authorize]
    public class AddressApiController : ControllerBase
    {
        private readonly ASPDbContext _context;

        public AddressApiController(ASPDbContext context)
        {
            _context = context;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var address = await _context.ShippingAddresses.FirstOrDefaultAsync(a => a.AddressId == id && a.UserId == userId);
            
            if (address == null) return NotFound();
            
            return Ok(new {
                addressId = address.AddressId,
                fullName = address.FullName,
                phone = address.Phone,
                addressLine = address.AddressLine,
                city = address.City,
                district = address.District,
                ward = address.Ward,
                isDefault = address.IsDefault
            });
        }

        public class AddressDto
        {
            public int AddressId { get; set; }
            public string FullName { get; set; }
            public string Phone { get; set; }
            public string City { get; set; }
            public string District { get; set; }
            public string Ward { get; set; }
            public string AddressLine { get; set; }
            public bool IsDefault { get; set; }
        }

        [HttpPost("save")]
        public async Task<IActionResult> Save([FromBody] AddressDto model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (string.IsNullOrEmpty(model.FullName) || string.IsNullOrEmpty(model.Phone) || 
                string.IsNullOrEmpty(model.City) || string.IsNullOrEmpty(model.District) || 
                string.IsNullOrEmpty(model.Ward) || string.IsNullOrEmpty(model.AddressLine))
            {
                return BadRequest(new { success = false, message = "Vui lòng điền đầy đủ thông tin" });
            }

            if (model.IsDefault)
            {
                var others = await _context.ShippingAddresses.Where(a => a.UserId == userId).ToListAsync();
                foreach (var addr in others) addr.IsDefault = false;
            }

            if (model.AddressId > 0)
            {
                // Update
                var existing = await _context.ShippingAddresses.FirstOrDefaultAsync(a => a.AddressId == model.AddressId && a.UserId == userId);
                if (existing == null) return NotFound();

                existing.FullName = model.FullName;
                existing.Phone = model.Phone;
                existing.City = model.City;
                existing.District = model.District;
                existing.Ward = model.Ward;
                existing.AddressLine = model.AddressLine;
                existing.IsDefault = model.IsDefault;

                _context.ShippingAddresses.Update(existing);
            }
            else
            {
                // Add
                var newAddress = new ShippingAddress
                {
                    UserId = userId,
                    FullName = model.FullName,
                    Phone = model.Phone,
                    City = model.City,
                    District = model.District,
                    Ward = model.Ward,
                    AddressLine = model.AddressLine,
                    IsDefault = model.IsDefault
                };
                _context.ShippingAddresses.Add(newAddress);
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Lưu địa chỉ thành công!" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var address = await _context.ShippingAddresses.FirstOrDefaultAsync(a => a.AddressId == id && a.UserId == userId);
            
            if (address == null) return NotFound(new { success = false, message = "Không tìm thấy địa chỉ" });
            
            _context.ShippingAddresses.Remove(address);
            await _context.SaveChangesAsync();
            
            return Ok(new { success = true, message = "Đã xóa địa chỉ" });
        }
    }
}
