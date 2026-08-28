using AdventureWorks.BlazorApp.Data;
using Shouldly;

namespace AdventureWorks.Tests;

public class CustomerProfileTests
{
    [Fact]
    public void DisplayName_PrefersCompanyName()
    {
        var customer = new CustomerProfile
        {
            CustomerID = 1,
            CompanyName = "Bike World",
            FirstName = "Ada",
            LastName = "Lovelace",
        };

        customer.DisplayName.ShouldBe("Bike World");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DisplayName_FallsBackToPersonName_WhenCompanyNameIsMissing(string? companyName)
    {
        var customer = new CustomerProfile
        {
            CustomerID = 1,
            CompanyName = companyName,
            FirstName = "Ada",
            LastName = "Lovelace",
        };

        customer.DisplayName.ShouldBe("Ada Lovelace");
    }

    [Fact]
    public void ContactName_TrimsMissingNameParts()
    {
        var customer = new CustomerProfile { CustomerID = 1, FirstName = "Ada" };

        customer.ContactName.ShouldBe("Ada");
    }
}
