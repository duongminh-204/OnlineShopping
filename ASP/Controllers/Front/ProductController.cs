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
    }
}