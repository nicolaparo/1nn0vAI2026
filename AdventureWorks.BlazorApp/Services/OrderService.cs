using AdventureWorks.BlazorApp.Data;
using Microsoft.EntityFrameworkCore;

namespace AdventureWorks.BlazorApp.Services;

public class OrderService(AdventureWorksDbContext dbContext)
{
    public async Task<IReadOnlyList<OrderSummary>> GetOrdersAsync(
        int take = 50,
        CancellationToken cancellationToken = default) =>
        await dbContext.SalesOrderHeaders
            .AsNoTracking()
            .OrderByDescending(o => o.OrderDate)
            .ThenByDescending(o => o.SalesOrderID)
            .Take(take)
            .Select(QueryProjections.ToOrderSummary)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OrderSummary>> GetOrdersByCustomerAsync(
        int customerId,
        CancellationToken cancellationToken = default) =>
        await dbContext.SalesOrderHeaders
            .AsNoTracking()
            .Where(o => o.CustomerID == customerId)
            .OrderByDescending(o => o.OrderDate)
            .ThenByDescending(o => o.SalesOrderID)
            .Select(QueryProjections.ToOrderSummary)
            .ToListAsync(cancellationToken);

    public async Task<int> GetOrderCountAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SalesOrderHeaders.CountAsync(cancellationToken);

    public async Task<decimal> GetTotalRevenueAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SalesOrderHeaders.SumAsync(o => o.TotalDue, cancellationToken);
}
