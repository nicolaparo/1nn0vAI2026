using AdventureWorks.BlazorApp.Services;
using Shouldly;

namespace AdventureWorks.Tests.Integration;

[Collection(AdventureWorksCollection.Name)]
public class OrderServiceTests(AdventureWorksContainerFixture fixture)
{
    [Fact]
    public async Task GetOrdersAsync_ReturnsMostRecentOrdersFirstAsync()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new OrderService(dbContext);

        var orders = await service.GetOrdersAsync(10);

        orders.Count.ShouldBe(10);
        orders.Select(o => o.OrderDate).ShouldBe(orders.Select(o => o.OrderDate).OrderDescending());
        orders.ShouldAllBe(o => o.TotalDue > 0);
    }

    [Fact]
    public async Task GetOrdersAsync_ResolvesTheCustomerNameAsync()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new OrderService(dbContext);

        var orders = await service.GetOrdersAsync(25);

        orders.ShouldAllBe(o => !string.IsNullOrWhiteSpace(o.CustomerName));
    }

    [Fact]
    public async Task GetOrdersByCustomerAsync_ReturnsOnlyThatCustomersOrdersAsync()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new OrderService(dbContext);
        var customerId = (await service.GetOrdersAsync(1)).Single().CustomerID;

        var orders = await service.GetOrdersByCustomerAsync(customerId);

        orders.ShouldNotBeEmpty();
        orders.ShouldAllBe(o => o.CustomerID == customerId);
    }

    [Fact]
    public async Task GetOrdersByCustomerAsync_ReturnsEmptyForUnknownCustomerAsync()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new OrderService(dbContext);

        var orders = await service.GetOrdersByCustomerAsync(int.MaxValue);

        orders.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetOrderCountAsync_MatchesTheSalesOrderHeaderTableAsync()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new OrderService(dbContext);

        var count = await service.GetOrderCountAsync();

        count.ShouldBe(31465);
    }

    [Fact]
    public async Task GetTotalRevenueAsync_ReturnsThePositiveSumOfTotalDueAsync()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new OrderService(dbContext);

        var revenue = await service.GetTotalRevenueAsync();

        revenue.ShouldBeGreaterThan(100_000_000m);
    }
}
