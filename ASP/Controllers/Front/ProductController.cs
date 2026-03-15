using ASP.Models.Domains;
using ASP.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 
using System.Threading.Tasks;
using OfficeOpenXml;
using Microsoft.AspNetCore.Authorization;
namespace ASP.Controllers.Front
{
    public class ProductController : Controller
    {
        private readonly ProductRepositoryInterface _productRepository;
        private readonly ProductImageRepository _productImageRepository;
        public ProductController(ProductRepositoryInterface productRepository, ProductImageRepository productImageRepository    )
        {
            _productRepository = productRepository;
            _productImageRepository = productImageRepository;
        }

        public IActionResult Index(int? category = null)
        {
            var products = _productRepository.GetAllProducts();
            if (category.HasValue)
            {
                products = products.Where(p => p.CategoryId == category.Value).ToList();
            }

            ViewBag.SelectedCategory = category;
            return View("~/Views/Front/Products/Index.cshtml", products);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _productRepository.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var viewModel = new ProductDetailViewModel
            {
                Product = product,
                Images = product.ProductImages?.ToList() ?? new List<ProductImage>(),
                DefaultVariant = product.ProductVariants?.OrderBy(v => v.VariantId).FirstOrDefault(),
            };

            viewModel.MainImageUrl = product.ProductImages
                ?.FirstOrDefault(x => x.IsMain)?.ImageUrl
                ?? product.ProductImages?.FirstOrDefault()?.ImageUrl
                ?? "/images/no-image.jpg";

            viewModel.CurrentPrice = viewModel.DefaultVariant?.Price ?? 0;

           

            return View("~/Views/Front/Products/ProductDetail.cshtml",viewModel);
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ImportProductsFromFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn file Excel!";
                return RedirectToAction("Index");
            }

            var list = new List<Product>();

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
              
                    using (var package = new ExcelPackage(stream))
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                        var rowCount = worksheet.Dimension?.Rows ?? 0;

                        if (rowCount < 2)
                        {
                            TempData["Error"] = "File Excel không có dữ liệu hoặc sai định dạng!";
                            return RedirectToAction("Index");
                        }

                        for (int row = 2; row <= rowCount; row++)
                        {
                         
                            var productName = worksheet.Cells[row, 1].Value?.ToString();
                            if (string.IsNullOrWhiteSpace(productName)) continue;

                            var product = new Product
                            {
                                ProductName = productName,
                                CategoryId = int.Parse(worksheet.Cells[row, 2].Value?.ToString() ?? "1"),
                                Description = worksheet.Cells[row, 3].Value?.ToString() ?? "",
                                Quantity = int.Parse(worksheet.Cells[row, 4].Value?.ToString() ?? "0"),
                                IsActive = true,
                                ProductVariants = new List<ProductVariant>
                        {
                            new ProductVariant
                            {
                                SKU = "SKU-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                                Price = decimal.Parse(worksheet.Cells[row, 5].Value?.ToString() ?? "0"),
                                QuantityVariants = int.Parse(worksheet.Cells[row, 4].Value?.ToString() ?? "0"),
                                IsActive = true
                            }
                        }
                            };
                            list.Add(product);
                        }
                    }
                }

                if (list.Any())
                {
                    await _productRepository.ImportProductsAsync(list);
                    TempData["Success"] = $"Đã nhập thành công {list.Count} sản phẩm!";
                }
                else
                {
                    TempData["Error"] = "Không tìm thấy dữ liệu hợp lệ trong file!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi xử lý file: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        public IActionResult DownloadTemplate()
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Template");

                // Header
                worksheet.Cells[1, 1].Value = "ProductName";
                worksheet.Cells[1, 2].Value = "CategoryID";
                worksheet.Cells[1, 3].Value = "Description";
                worksheet.Cells[1, 4].Value = "Quantity";
                worksheet.Cells[1, 5].Value = "Price";

                // Định dạng Header đen trắng
                using (var range = worksheet.Cells[1, 1, 1, 5])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Black);
                    range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                }

                worksheet.Cells.AutoFitColumns();
                var fileContents = package.GetAsByteArray();
                return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Template_Import.xlsx");
            }
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProductImage(int productId, IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0) return BadRequest();

         
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products", fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

           
            var newImage = new ProductImage
            {
                ProductId = productId,
                ImageUrl = "/images/" + fileName,
                IsMain = false 
            };

          
            await _productImageRepository.AddImageAsync(newImage);

            return Json(new { success = true, newImageUrl = newImage.ImageUrl });
        }
    }
}
