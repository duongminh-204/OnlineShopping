using Microsoft.EntityFrameworkCore;
using ASP.Models.ASPModel;

namespace ASP.Models.Domains
{
    public class CategoryRepository : CategoryRepositoryInterface   
    {
        private readonly ASPDbContext _context;

        public CategoryRepository(ASPDbContext context)
        {
            _context = context;
        }
        public IEnumerable<Category> GetAllCategories()
        {
            return _context.Categories
                .OrderBy(c => c.CategoryName)
                .ToList();
        }
    }
}
