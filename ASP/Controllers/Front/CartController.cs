using ASP.Models.Domains;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ASP.Controllers.Front
{
    public class CartController : Controller
    {
        private readonly CartRepositoyInterface _cartRepo;
        private readonly CartItemRepositoryInterface _cartItemRepo;
        private readonly ASP.Models.ASPModel.ASPDbContext _context;

        public CartController(
            CartRepositoyInterface cartRepo,
            CartItemRepositoryInterface cartItemRepo,
            ASP.Models.ASPModel.ASPDbContext context)
        {
            _cartRepo = cartRepo;
            _cartItemRepo = cartItemRepo;
            _context = context;
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }


        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();

            if (string.IsNullOrEmpty(userId))
                return Redirect("/Login");

            var cart = await _cartRepo.GetCartWithItemsAsync(userId);

            if (cart == null)
            {
                // Return empty cart view so the friendly empty-state UI is shown
                var emptyCart = new Cart
                {
                    CartItems = new System.Collections.Generic.List<CartItem>()
                };

                ViewBag.CartItemCount = 0;
                ViewBag.Addresses = await _context.ShippingAddresses
                    .Where(s => s.UserId == userId)
                    .Include(a => a.User)
                    .ToListAsync();

                return View("~/Views/Front/Carts/Index.cshtml", emptyCart);
            }

            cart.CartItems = GetActiveCartItems(cart);

            ViewBag.CartItemCount = cart.CartItems.Sum(ci => ci.Quantity);
            ViewBag.Addresses = await _context.ShippingAddresses
                .Where(s => s.UserId == userId)
                .Include(a => a.User)
                .ToListAsync();

            return View("~/Views/Front/Carts/Index.cshtml", cart);
        }

        private async Task<(bool IsValid, string? Message, int AvailableStock)> ValidateStockAsync(int variantId, int requestedQuantity)
        {
            var variant = await _context.ProductVariants
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v => v.VariantId == variantId);

            if (variant == null)
            {
                return (false, "Không tìm thấy biến thể sản phẩm", 0);
            }

            if (!variant.IsActive)
            {
                return (false, "Biến thể này hiện không còn hoạt động", 0);
            }

            if (variant.Product == null || !variant.Product.IsActive)
            {
                return (false, "Sản phẩm này hiện không còn hoạt động", 0);
            }

            var availableStock = Math.Min(variant.QuantityVariants, variant.Product.Quantity);

            if (availableStock <= 0)
            {
                return (false, "Biến thể này đã hết hàng", 0);
            }

            if (requestedQuantity > availableStock)
            {
                return (false, $"Số lượng vượt quá tồn kho. Chỉ còn {availableStock} sản phẩm.", availableStock);
            }

            return (true, null, availableStock);
        }

        private async Task<(bool IsValid, string? Message)> ValidateCartItemsAsync(IReadOnlyList<CartItem> cartItems)
        {
            var errors = new List<string>();

            foreach (var item in cartItems)
            {
                if (item.Quantity <= 0)
                {
                    errors.Add("Một sản phẩm trong giỏ hàng có số lượng không hợp lệ.");
                    continue;
                }

                var stockValidation = await ValidateStockAsync(item.VariantId, item.Quantity);
                if (!stockValidation.IsValid)
                {
                    var productName = item.ProductVariant?.Product?.ProductName ?? "Sản phẩm";
                    errors.Add($"{productName}: {stockValidation.Message}");
                }
            }

            return (errors.Count == 0, errors.FirstOrDefault());
        }

        private static List<CartItem> GetActiveCartItems(Cart? cart)
        {
            return cart?.CartItems?
                .Where(ci =>
                    ci.ProductVariant != null &&
                    ci.ProductVariant.IsActive &&
                    ci.ProductVariant.Product != null &&
                    ci.ProductVariant.Product.IsActive)
                .ToList()
                ?? new List<CartItem>();
        }

        [HttpGet]
        public async Task<IActionResult> ValidateBeforeCheckout()
        {
            var userId = GetUserId();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Vui lòng đăng nhập" });

            var cart = await _cartRepo.GetCartWithItemsAsync(userId);
            var activeItems = GetActiveCartItems(cart);

            if (!activeItems.Any())
                return BadRequest(new { message = "Giỏ hàng trống" });

            var (isValid, message) = await ValidateCartItemsAsync(activeItems);

            if (!isValid)
                return BadRequest(new { message = message ?? "Một số sản phẩm trong giỏ hàng không còn đủ số lượng." });

            return Ok(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartModel model)
        {
            var userId = GetUserId();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Vui lòng đăng nhập" });

            if (model.Quantity < 1)
                return BadRequest(new { message = "Số lượng phải lớn hơn 0" });

            var stockValidation = await ValidateStockAsync(model.VariantId, model.Quantity);

            if (!stockValidation.IsValid)
                return BadRequest(new { message = stockValidation.Message });

            var cart = await _cartRepo.GetOrCreateCartAsync(userId);

            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.CartId && ci.VariantId == model.VariantId);

            var currentQtyInCart = existingItem?.Quantity ?? 0;

            if (currentQtyInCart + model.Quantity > stockValidation.AvailableStock)
            {
                return BadRequest(new
                {
                    message = $"Số lượng vượt quá tồn kho. Chỉ còn {stockValidation.AvailableStock} sản phẩm."
                });
            }

            await _cartItemRepo.AddToCartAsync(
                cart.CartId,
                model.VariantId,
                model.Quantity
            );

            await _cartRepo.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Đã thêm vào giỏ hàng"
            });
        }


        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            var userId = GetUserId();

            if (string.IsNullOrEmpty(userId))
                return Json(0);

            var cart = await _cartRepo.GetCartWithItemsAsync(userId);

            var activeItems = cart?.CartItems?
                .Where(ci =>
                    ci.ProductVariant != null &&
                    ci.ProductVariant.IsActive &&
                    ci.ProductVariant.Product != null &&
                    ci.ProductVariant.Product.IsActive)
                .ToList()
                ?? new List<CartItem>();

            int count = activeItems.Sum(i => i.Quantity);

            return Json(count);
        }


        [HttpPost]
        public async Task<IActionResult> RemoveItem([FromBody] RemoveItemModel model)
        {
            var userId = GetUserId();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var cart = await _cartRepo.GetCartWithItemsAsync(userId);

            if (cart == null || !cart.CartItems.Any())
                return BadRequest("Giỏ hàng trống");

            var item = cart.CartItems
                .FirstOrDefault(x => x.CartItemId == model.CartItemId);

            if (item == null)
                return BadRequest("Không tìm thấy sản phẩm");

            _cartItemRepo.Delete(item);

            await _cartRepo.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Đã xoá sản phẩm"
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateItem([FromBody] UpdateCartItemModel model)
        {
            var userId = GetUserId();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Vui lòng đăng nhập" });

            if (model.Quantity < 1)
                return BadRequest(new { message = "Số lượng phải lớn hơn 0" });

            var cart = await _cartRepo.GetCartWithItemsAsync(userId);

            if (cart == null || !cart.CartItems.Any())
                return BadRequest(new { message = "Giỏ hàng trống" });

            var item = cart.CartItems.FirstOrDefault(x => x.CartItemId == model.CartItemId);

            if (item == null)
                return BadRequest(new { message = "Không tìm thấy sản phẩm trong giỏ" });

            if (item.ProductVariant == null || !item.ProductVariant.IsActive ||
                item.ProductVariant.Product == null || !item.ProductVariant.Product.IsActive)
            {
                return BadRequest(new { message = "Sản phẩm này hiện không còn khả dụng" });
            }

            var stockValidation = await ValidateStockAsync(item.VariantId, model.Quantity);

            if (!stockValidation.IsValid)
            {
                return BadRequest(new { message = stockValidation.Message });
            }

            await _cartItemRepo.UpdateQuantityAsync(model.CartItemId, model.Quantity);
            await _cartRepo.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Cập nhật số lượng thành công"
            });
        }

        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            var userId = GetUserId();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Vui lòng đăng nhập" });

            var cart = await _cartRepo.GetCartWithItemsAsync(userId);

            if (cart == null || !cart.CartItems.Any())
                return BadRequest(new { message = "Giỏ hàng trống" });

            await _cartItemRepo.ClearCartAsync(cart.CartId);
            await _cartRepo.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Đã xóa toàn bộ giỏ hàng"
            });
        }
    }


    public class AddToCartModel
    {
        public int VariantId { get; set; }

        public int Quantity { get; set; } = 1;
    }

    public class RemoveItemModel
    {
        public int CartItemId { get; set; }
    }
    public class UpdateCartItemModel
    {
        public int CartItemId { get; set; }
        public int Quantity { get; set; }
    }
}