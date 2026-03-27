namespace ASP.Models.ViewModels
{
    public class HomeCategoryItemViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = "/images/no-image.jpg";
    }
}