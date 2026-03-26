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
                .Include(p => p.ProductVariants)
                .OrderByDescending(p => p.ProductId)
                .ToList();
        }

        public IEnumerable<Product> GetBestSellingProducts(int take = 8)
        {
            return _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductVariants)
                .Where(p => p.IsActive)
                .Take(take)
                .ToList();
        }

        public IEnumerable<Product> GetNewArrivals(int take = 4)
        {
            return _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductVariants)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.ProductId)
                .Take(take)
                .ToList();
        }
    
    public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductVariants)
                .FirstOrDefaultAsync(p => p.ProductId == id);
        }
        public async Task ImportProductsAsync(List<Product> products)
        {
            await _context.Products.AddRangeAsync(products);
            await _context.SaveChangesAsync();
        }

        // chuyển query filter từ Controller sang Repo
        public IQueryable<Product> GetProducts(string? filter, int? categoryId)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductVariants)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter))
            {
                query = query.Where(p => p.ProductName.Contains(filter));
            }

            if (categoryId != null && categoryId > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }

            return query;
        }

        // lấy product theo id
        public Product? GetById(int id)
        {
            return _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ProductVariants)
                .FirstOrDefault(p => p.ProductId == id);
        }

        // thêm product
        public void Add(Product product)
        {
            _context.Products.Add(product);
            _context.SaveChanges();
        }

        // update product
        public void Update(Product product)
        {
            _context.Products.Update(product);
            _context.SaveChanges();
        }

        // delete product
        public void Delete(Product product)
        {
            _context.Products.Remove(product);
            _context.SaveChanges();
        }

        // check product có image
        public bool HasImage(int productId)
        {
            return _context.ProductImages.Any(x => x.ProductId == productId);
        }

        // lấy categories (để controller không gọi context)
        public List<Category> GetCategories()
        {
            return _context.Categories.ToList();
        }
    }
}