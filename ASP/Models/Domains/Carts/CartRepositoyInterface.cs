namespace ASP.Models.Domains
{
    public interface CartRepositoyInterface
    {
        Task<Cart> GetOrCreateCartAsync(string customerId);
        Task SaveChangesAsync();
        Task<Cart?> GetCartWithItemsAsync(string customerId);
    }
}
