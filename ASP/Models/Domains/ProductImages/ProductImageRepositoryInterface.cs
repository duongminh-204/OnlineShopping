namespace ASP.Models.Domains
{
    public interface ProductImageRepositoryInterface
    {
        List<ProductImage> GetImagesByProductId(int productId);

        ProductImage? GetImageById(int id);

        bool HasMainImage(int productId);

        bool HasAnyImage(int productId);

        Task AddImageAsync(ProductImage image);

        void DeleteImage(ProductImage image);

        void SetMainImage(ProductImage img);

        Product? GetProduct(int productId);

        void UpdateProduct(Product product);
    }
}
