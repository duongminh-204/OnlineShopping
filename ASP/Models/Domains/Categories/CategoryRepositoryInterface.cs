using Microsoft.EntityFrameworkCore;
using ASP.Models.ASPModel;
namespace ASP.Models.Domains
{
    public interface CategoryRepositoryInterface
    {
      IEnumerable<Category> GetAllCategories(); 
    }
}
