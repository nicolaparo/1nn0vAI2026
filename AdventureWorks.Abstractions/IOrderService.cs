namespace AdventureWorks.Abstractions;

public interface IOrderService
{
    IQueryable<OrderSummary> GetOrders(int? customerId = null);
    Task<int> GetOrderCountByCustomerAsync(int customerId, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalRevenueByCustomerAsync(int customerId, CancellationToken cancellationToken = default);
    Task<int> GetOrderCountAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetTotalRevenueAsync(CancellationToken cancellationToken = default);
}
