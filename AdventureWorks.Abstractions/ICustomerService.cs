namespace AdventureWorks.Abstractions;

public interface ICustomerService
{
    IQueryable<CustomerProfile> GetCustomers(string? companyName = null);
    Task<CustomerProfile?> GetCustomerAsync(int customerId, CancellationToken cancellationToken = default);
    Task<int> GetCustomerCountAsync(CancellationToken cancellationToken = default);
}
