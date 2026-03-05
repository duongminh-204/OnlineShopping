namespace ASP.Models.Domains
{
    public interface ProductRepositoryInterface
    {
        IEnumerable<Product> GetAllProducts();
     
    }
}