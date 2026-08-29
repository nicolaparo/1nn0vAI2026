using AdventureWorks.Abstractions;
using AdventureWorks.BlazorApp.Data;
using Microsoft.EntityFrameworkCore;

namespace AdventureWorks.BlazorApp.Services;

public class OrderService(AdventureWorksDbContext dbContext) : IOrderService
{
    public IQueryable<OrderSummary> GetOrders(int? customerId = null)
    {
        var query = dbContext.SalesOrderHeaders
            .AsNoTracking()
            .Select(QueryProjections.ToOrderSummary);

        return customerId is null ? query : query.Where(o => o.CustomerID == customerId);
    }

    public async Task<int> GetOrderCountByCustomerAsync(
        int customerId,
        CancellationToken cancellationToken = default) =>
        await dbContext.SalesOrderHeaders
            .AsNoTracking()
            .CountAsync(o => o.CustomerID == customerId, cancellationToken);

    public async Task<decimal> GetTotalRevenueByCustomerAsync(
        int customerId,
        CancellationToken cancellationToken = default) =>
        await dbContext.SalesOrderHeaders
            .AsNoTracking()
            .Where(o => o.CustomerID == customerId)
            .SumAsync(o => o.TotalDue, cancellationToken);

    public async Task<int> GetOrderCountAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SalesOrderHeaders.CountAsync(cancellationToken);

    public async Task<decimal> GetTotalRevenueAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SalesOrderHeaders.SumAsync(o => o.TotalDue, cancellationToken);
}
