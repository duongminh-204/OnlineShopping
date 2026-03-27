using Microsoft.EntityFrameworkCore;
using ASP.Models.ASPModel;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

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
            try
            {
                return GetProductsFromServer().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi lấy dữ liệu từ Server: {ex.Message}");
              
                return new List<Product>();
            }
        }

       
        private async Task<IEnumerable<Product>> GetProductsFromServer()
        {
            string serverIp = "127.0.0.1";   
            int serverPort = 5000;

            using TcpClient client = new TcpClient();
            await client.ConnectAsync(serverIp, serverPort);

            using NetworkStream stream = client.GetStream();

            
            string command = "GET_PRODUCTS";
            byte[] requestBytes = Encoding.UTF8.GetBytes(command);
            await stream.WriteAsync(requestBytes, 0, requestBytes.Length);

            
            byte[] buffer = new byte[65536]; 
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

            if (bytesRead == 0)
                return new List<Product>();

            string responseJson = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

       
            var response = JsonSerializer.Deserialize<JsonResponse>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (response?.success != true || response.products == null)
                return new List<Product>();

           
            var products = new List<Product>();

            foreach (var item in response.products)
            {
                var product = new Product
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                   
                };

                
                if (!string.IsNullOrEmpty(item.Category) && item.Category != "N/A")
                {
                    product.Category = new Category { CategoryName = item.Category };
                }

                products.Add(product);
            }

            return products;
        }

        public IEnumerable<Product> GetAllProducts1()
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


        //public IQueryable<Product> GetProducts(string? filter, int? categoryId)
        //{
        //    var query = _context.Products
        //        .Include(p => p.Category)
        //        .Include(p => p.ProductImages)
        //        .Include(p => p.ProductVariants)
        //        .AsQueryable();

        //    if (!string.IsNullOrEmpty(filter))
        //    {
        //        query = query.Where(p => p.ProductName.Contains(filter));
        //    }

        //    if (categoryId != null && categoryId > 0)
        //    {
        //        query = query.Where(p => p.CategoryId == categoryId);
        //    }

        //    return query;
        //}

        public IQueryable<Product> GetProducts(string? filter, int? categoryId)
        {
            string host = "127.0.0.1";
            int port = 5000;

            List<Product> products = new List<Product>();

            using (TcpClient client = new TcpClient(host, port))
            using (NetworkStream stream = client.GetStream())
            {
                string command = "GET_PRODUCTS_MANAGE";
                byte[] request = Encoding.UTF8.GetBytes(command);

                stream.Write(request, 0, request.Length);

                MemoryStream ms = new MemoryStream();
                byte[] buffer = new byte[4096];
                int bytesRead;

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, bytesRead);

                    if (!stream.DataAvailable)
                        break;
                }

                string json = Encoding.UTF8.GetString(ms.ToArray());

                if (bytesRead > 0)
                {
                    //string json = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                    var response = JsonSerializer.Deserialize<JsonResponse>(json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    if (response?.success == true && response.productManage != null)
                    {
                        foreach (var item in response.productManage)
                        {
                            var product = new Product
                            {
                                ProductId = item.ProductId,
                                ProductName = item.ProductName,
                                Quantity = item.Quantity,

                                Category = item.Category == null ? null : new Category
                                {
                                    CategoryId = item.Category.CategoryId,
                                    CategoryName = item.Category.CategoryName ?? ""
                                },

                                ProductImages = item.ProductImages?.Select(x => new ProductImage
                                    {
                                        ProductImageId = x.ProductImageId,
                                        ImageUrl = x.ImageUrl ?? "",
                                        IsMain = x.IsMain
                                    }).ToList() ?? new List<ProductImage>(),

                                ProductVariants = item.ProductVariants?.Select(v => new ProductVariant
                                    {
                                        VariantId = v.VariantId,
                                        Price = v.Price
                                        }).ToList() ?? new List<ProductVariant>()
                                    };

                            products.Add(product);
                        }
                    }
                }
            }

            var query = products.AsQueryable();

            // giữ nguyên logic filter như code cũ
            if (!string.IsNullOrEmpty(filter))
            {
                query = query.Where(p => p.ProductName.Contains(filter));
            }

            if (categoryId != null && categoryId > 0)
            {
                query = query.Where(p => p.Category != null && p.Category.CategoryId == categoryId);
            }

            return query;
        }

        public Product? GetById(int id)
        {
            return _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ProductVariants)
                .FirstOrDefault(p => p.ProductId == id);
        }

        public void Add(Product product)
        {
            _context.Products.Add(product);
            _context.SaveChanges();
        }

        public void Update(Product product)
        {
            _context.Products.Update(product);
            _context.SaveChanges();
        }

        public void Delete(Product product)
        {
            _context.Products.Remove(product);
            _context.SaveChanges();
        }

        public bool HasImage(int productId)
        {
            return _context.ProductImages.Any(x => x.ProductId == productId);
        }

        public List<Category> GetCategories()
        {
            return _context.Categories.ToList();
        }
    }

    
    public class JsonResponse
    {
        public bool success { get; set; }
        public string command { get; set; } = string.Empty;
        public int count { get; set; }
        public List<ProductDto> products { get; set; } = new();

        public List<ProductManageDto> productManage { get; set; } = new();
        public DateTime timestamp { get; set; }
    }

    public class ProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string MainImage { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    public class ProductManageDto
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
        public int CategoryId { get; set; }

        public CategoryDto? Category { get; set; }

        public List<ProductImageDto>? ProductImages { get; set; }
        public List<ProductVariantDto>? ProductVariants { get; set; }
    }

    public class ProductImageDto
    {
        public int ProductImageId { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsMain { get; set; }
    }

    public class ProductVariantDto
    {
        public int VariantId { get; set; }
        public decimal Price { get; set; }
    }
    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
    }
}