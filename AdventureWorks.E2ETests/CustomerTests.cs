using Microsoft.Playwright;

namespace AdventureWorks.E2ETests;

public class CustomerTests : AdventureWorksPageTest
{
    [Fact]
    public async Task Customers_ShowsTheCustomerGridAsync()
    {
        await Page.GotoAsync("/customers");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Customers" })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("customer-row")).ToHaveCountAsync(50);
    }

    [Fact]
    public async Task Customers_FiltersByCompanyNameAsync()
    {
        await Page.GotoAsync("/customers");
        await Expect(Page.GetByTestId("customer-row").First).ToBeVisibleAsync();

        await Page.GetByLabel("Filter by company name").FillAsync("Bike World");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync();

        await Expect(Page.GetByTestId("customer-row")).Not.ToHaveCountAsync(0);
        await Expect(Page.GetByTestId("customer-row").First).ToContainTextAsync("Bike World");
    }

    [Fact]
    public async Task Customers_ShowUnfilteredResultsWhenNothingMatchesAsync()
    {
        await Page.GotoAsync("/customers");
        await Expect(Page.GetByTestId("customer-row").First).ToBeVisibleAsync();

        await Page.GetByLabel("Filter by company name").FillAsync("no-such-company-xyz");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync();

        await Expect(Page.GetByText("No customers match the current filter.")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CustomerDetail_ShowsProfileOrderHistoryAndPlaceholderAsync()
    {
        await Page.GotoAsync("/customers");
        await Expect(Page.GetByTestId("customer-row").First).ToBeVisibleAsync();

        await Page.GetByTestId("customer-row").First.GetByRole(AriaRole.Link).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex(@"/customers/\d+$"));
        await Expect(Page.GetByText("Profile")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Order history")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("ui-customization-placeholder")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("ui-customization-placeholder"))
            .ToContainTextAsync("User UI Customization / Dynamic Component Placeholder");
    }

    [Fact]
    public async Task CustomerDetail_ShowsAWarningForAnUnknownCustomerAsync()
    {
        await Page.GotoAsync("/customers/2147483647");

        await Expect(Page.GetByText("was not found")).ToBeVisibleAsync();
    }
}
