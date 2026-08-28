using AdventureWorks.BlazorApp.Data;
using Microsoft.EntityFrameworkCore;

namespace AdventureWorks.BlazorApp.Services;

public class CustomerService(AdventureWorksDbContext dbContext)
{
    public async Task<IReadOnlyList<CustomerProfile>> SearchCustomersAsync(
        string? companyName,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Customers
            .AsNoTracking()
            .Select(QueryProjections.ToCustomerProfile);

        if (!string.IsNullOrWhiteSpace(companyName))
        {
            query = query.Where(c => c.CompanyName != null && EF.Functions.Like(c.CompanyName, $"%{companyName}%"));
        }

        return await query
            .OrderBy(c => c.CompanyName)
            .Take(take)
            .ToListAsync(cancellationToken);
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
