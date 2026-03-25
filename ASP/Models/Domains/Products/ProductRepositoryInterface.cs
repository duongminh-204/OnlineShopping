namespace ASP.Models.Domains
{
    public interface ProductRepositoryInterface
    {
        IEnumerable<Product> GetAllProducts();
        Task<Product?> GetProductByIdAsync(int id);
        Task ImportProductsAsync(List<Product> products);
        IQueryable<Product> GetProducts(string? filter, int? categoryId);

        Product? GetById(int id);

        void Add(Product product);

        void Update(Product product);

        void Delete(Product product);

        bool HasImage(int productId);

        List<Category> GetCategories();
    }
}