namespace ASP.Models.Domains
{
    public interface CartRepositoyInterface
    {
        Task<Cart> GetOrCreateCartAsync(int customerId);
        Task SaveChangesAsync();
    }
}
