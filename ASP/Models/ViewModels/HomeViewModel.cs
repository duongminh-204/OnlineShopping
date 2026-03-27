using ASP.Models.Domains;
using System.Collections.Generic;

namespace ASP.Models.ViewModels
{
    public class HomeViewModel
    {
        public List<HomeCategoryItemViewModel> Categories { get; set; } = new();
        public List<Product> PopularProducts { get; set; } = new();
        public List<Product> NewestProducts { get; set; } = new();
    }
}