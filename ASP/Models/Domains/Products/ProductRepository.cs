using Microsoft.EntityFrameworkCore;
using ASP.Models.ASPModel;

namespace ASP.Models.Domains
{
    public class ProductRepository : ProductRepositoryInterface
    {
        private readonly ASPDbContext _context;

        public ProductRepository(ASPDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Product> GetAllProducts()
        {
            return _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .OrderByDescending(p => p.ProductId)
                .ToList();
        }
    }
}