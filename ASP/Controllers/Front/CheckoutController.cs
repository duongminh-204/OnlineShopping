using ASP.Models.Admin.Accounts;
using ASP.Models.ASPModel;
using ASP.Models.Domains;
using ASP.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ASP.Controllers.Front
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ASPDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CheckoutController(ASPDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<(bool IsValid, string? ErrorMessage)> ValidateCartForOrderAsync(IReadOnlyList<CartItem> cartItems)
        {
            var errors = new List<string>();

            foreach (var item in cartItems)
            {
                if (item.Quantity <= 0)
                {
                    errors.Add("Một sản phẩm trong giỏ hàng có số lượng không hợp lệ.");
                    continue;
                }

                var variant = await _context.ProductVariants
                    .Include(v => v.Product)
                    .FirstOrDefaultAsync(v => v.VariantId == item.VariantId);

                if (variant == null)
                {
                    errors.Add("Một sản phẩm trong giỏ hàng không còn tồn tại.");
                    continue;
                }

                if (!variant.IsActive || variant.Product == null || !variant.Product.IsActive)
                {
                    errors.Add("Một sản phẩm trong giỏ hàng hiện không còn khả dụng.");
                    continue;
                }

                var productStock = variant.Product?.Quantity ?? int.MaxValue;
                var availableStock = Math.Min(variant.QuantityVariants, productStock);

                if (item.Quantity > availableStock)
                {
                    var productName = variant.Product?.ProductName ?? "Sản phẩm";
                    errors.Add($"{productName} chỉ còn {availableStock} sản phẩm trong kho.");
                }
            }

            return (errors.Count == 0, errors.FirstOrDefault());
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 🔹 Lấy cart
            var cart = await _context.Carts
                .Include(c => c.User)
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                            .ThenInclude(p => p.ProductImages)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            var cartItems = cart.CartItems?.ToList() ?? new List<CartItem>();
            var (isValid, errorMessage) = await ValidateCartForOrderAsync(cartItems);

            if (!isValid)
            {
                TempData["Error"] = errorMessage ?? "Một số sản phẩm trong giỏ hàng không còn đủ số lượng.";
                return RedirectToAction("Index", "Cart");
            }

            var addresses = await _context.ShippingAddresses
                .Where(s => s.UserId == userId)
                .Include(a => a.User) 
                .ToListAsync();

        
            if (!addresses.Any())
            {
                TempData["Error"] = "Vui lòng thêm địa chỉ giao hàng trước!";
                return RedirectToAction("Index", "AddAddress");
            }

            var defaultAddress = addresses.FirstOrDefault(a => a.IsDefault)
                                 ?? addresses.FirstOrDefault();

            var user = await _userManager.GetUserAsync(User);

            var vm = new CheckoutViewModel
            {
                CartItems = cart.CartItems?.ToList() ?? new List<CartItem>(),
                Addresses = addresses,
                Address = defaultAddress,
                user = user,
                TotalAmount = cart.CartItems.Sum(x => x.Quantity * (x.ProductVariant?.Price ?? 0))
            };

            return View("~/Views/Front/Checkout/Index.cshtml", vm);
        }

     
        [HttpPost]
        public async Task<IActionResult> PlaceOrder()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

           
            var cart = await _context.Carts
                .Include(c => c.User)
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng trống!";
                return RedirectToAction("Index", "Cart");
            }

            var cartItems = cart.CartItems?.ToList() ?? new List<CartItem>();
            var (isValid, errorMessage) = await ValidateCartForOrderAsync(cartItems);

            if (!isValid)
            {
                TempData["Error"] = errorMessage ?? "Một số sản phẩm trong giỏ hàng không còn đủ số lượng.";
                return RedirectToAction("Index", "Cart");
            }

            if (!int.TryParse(Request.Form["addressId"], out int addressId))
            {
                TempData["Error"] = "Vui lòng chọn địa chỉ giao hàng hợp lệ!";
                return RedirectToAction("Index");
            }

            var shippingAddress = await _context.ShippingAddresses
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.UserId == userId && a.AddressId == addressId);
           
            if (shippingAddress == null)
            {
                TempData["Error"] = "Không tìm thấy địa chỉ giao hàng!";
                return RedirectToAction("Index");
            }

          
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                CreatedBy = cart.User?.FullName ?? "Unknown",
                ShippingAddress = shippingAddress,
                Status = "Pending",
                TotalAmount = cart.CartItems.Sum(x => x.Quantity * (x.ProductVariant?.Price ?? 0))
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            
            foreach (var item in cart.CartItems)
            {
                if (item.ProductVariant == null)
                    continue; 

                var orderDetail = new OrderDetail
                {
                    OrderId = order.OrderId,
                    VariantId = item.VariantId,
                    Quantity = item.Quantity,
                    UnitPrice = item.ProductVariant.Price
                };

                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductId == item.ProductVariant.ProductId);

                var variant = await _context.ProductVariants
                    .FirstOrDefaultAsync(pv => pv.VariantId == item.VariantId);

                if (variant != null)
                {
                    variant.QuantityVariants -= item.Quantity;
                }

                if (product != null)
                {
                    product.Quantity -= item.Quantity;
                }

                _context.OrderDetails.Add(orderDetail);
            }

            
            _context.CartItems.RemoveRange(cart.CartItems);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đặt hàng thành công!";
            return RedirectToAction("Success");
        }

      
        public IActionResult Success()
        {
            return View("~/Views/Front/Checkout/Success.cshtml");
        }
    }
}