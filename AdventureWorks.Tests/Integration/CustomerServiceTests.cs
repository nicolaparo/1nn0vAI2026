using AdventureWorks.BlazorApp.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace AdventureWorks.Tests.Integration;

[Collection(AdventureWorksCollection.Name)]
public class CustomerServiceTests(AdventureWorksContainerFixture fixture)
{
    [Fact]
    public async Task SearchCustomersAsync_ReturnsStoresAndIndividualsWithContactDetailsAsync()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new CustomerService(dbContext);

        var customers = await service.GetCustomers().Take(25).ToListAsync();

        customers.Count.ShouldBe(25);
        customers.ShouldAllBe(c => c.CustomerID > 0);
        customers.ShouldAllBe(c => !string.IsNullOrWhiteSpace(c.DisplayName));
    }

    [Fact]
    public async Task SearchCustomersAsync_FiltersByCompanyNameAsync()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new CustomerService(dbContext);

        var customers = await service.GetCustomers("Bike").ToListAsync();

        customers.ShouldNotBeEmpty();
        customers.ShouldAllBe(c => c.CompanyName!.Contains("Bike", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchCustomersAsync_IsOrderedByCompanyNameAsync()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new CustomerService(dbContext);

        var customers = await service.GetCustomers("Bike").OrderBy(c => c.CompanyName).Take(10).ToListAsync();

        customers.Select(c => c.CompanyName).ShouldBe(customers.Select(c => c.CompanyName).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task SearchCustomersAsync_HonoursTakeAsync()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new CustomerService(dbContext);

        var customers = await service.GetCustomers().Take(3).ToListAsync();

        customers.Count.ShouldBe(3);
    }

    [Fact]
    public async Task GetCustomerAsync_ReturnsStoreCustomerAsync()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new CustomerService(dbContext);
        var expected = (await service.GetCustomers("Bike").Take(1).ToListAsync()).Single();

        var customer = await service.GetCustomerAsync(expected.CustomerID);

        customer.ShouldNotBeNull();
        customer.CustomerID.ShouldBe(expected.CustomerID);
        customer.CompanyName.ShouldBe(expected.CompanyName);
    }

    [Fact]
    public async Task GetCustomerAsync_ReturnsNullForUnknownCustomerAsync()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new CustomerService(dbContext);

        var customer = await service.GetCustomerAsync(int.MaxValue);

        customer.ShouldBeNull();
    }

    [Fact]
    public async Task GetCustomerCountAsync_MatchesTheCustomerTableAsync()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new CustomerService(dbContext);

        var count = await service.GetCustomerCountAsync();

        count.ShouldBe(19820);
    }
}
