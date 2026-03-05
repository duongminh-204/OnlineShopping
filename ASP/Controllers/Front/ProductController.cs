using Microsoft.AspNetCore.Mvc;
using ASP.Models.Domains;

namespace ASP.Controllers.Front
{
    public class ProductController : Controller
    {
        private readonly ProductRepositoryInterface _productRepository;

        public ProductController(ProductRepositoryInterface productRepository)
        {
            _productRepository = productRepository;
        }

       
        public static string GetCategoryName(int categoryId)
        {
            return categoryId switch
            {
                1 => "Áo Thun / Hoodie",
                2 => "Áo Sơ Mi",
                3 => "Quần Jeans / Short",
                4 => "Váy Nữ",
                5 => "Áo Khoác",
                _ => $"Danh mục {categoryId}"
            };
        }

        public static string GetColorHex(string? colorName)
        {
            return (colorName?.ToLower() ?? "") switch
            {
                "black" => "#1a1a1a",
                "white" => "#f8f9fa",
                "gray" => "#6c757d",
                "blue" => "#0d6efd",
                "dark blue" => "#0a2540",
                "pink" => "#e83e8c",
                "beige" => "#e3d5ca",
                "brown" => "#8d5524",
                _ => "#6c757d"
            };
        }
        

        public IActionResult Index()
        {
            var products = _productRepository.GetAllProducts();
            return View("~/Views/Front/Products/Index.cshtml", products);
        }
    }
}