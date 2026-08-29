using AdventureWorks.Abstractions;
using AdventureWorks.BlazorApp.Data;
using Microsoft.EntityFrameworkCore;

namespace AdventureWorks.BlazorApp.Services;

public class CustomerService(AdventureWorksDbContext dbContext) : ICustomerService
{
    public IQueryable<CustomerProfile> GetCustomers(string? companyName = null)
    {
        var query = dbContext.Customers
            .AsNoTracking()
            .Select(QueryProjections.ToCustomerProfile);

        return string.IsNullOrWhiteSpace(companyName)
            ? query
            : query.Where(c => c.CompanyName != null && EF.Functions.Like(c.CompanyName, $"%{companyName}%"));
    }

    public async Task<CustomerProfile?> GetCustomerAsync(int customerId, CancellationToken cancellationToken = default) =>
        await dbContext.Customers
            .AsNoTracking()
            .Where(c => c.CustomerID == customerId)
            .Select(QueryProjections.ToCustomerProfile)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<int> GetCustomerCountAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Customers.CountAsync(cancellationToken);
}
