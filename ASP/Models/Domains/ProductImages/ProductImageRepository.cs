using ASP.Models.ASPModel;

namespace ASP.Models.Domains
{
    
        public class ProductImageRepository : ProductImageRepositoryInterface
        {
            private readonly ASPDbContext _context; 
            public ProductImageRepository(ASPDbContext context) => _context = context;

            public async Task AddImageAsync(ProductImage image)
            {
                await _context.ProductImages.AddAsync(image);
                await _context.SaveChangesAsync();
            }
        }
    }

