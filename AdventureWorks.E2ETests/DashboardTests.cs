using Microsoft.Playwright;

namespace AdventureWorks.E2ETests;

public class DashboardTests : AdventureWorksPageTest
{
    [Fact]
    public async Task Dashboard_ShowsFourKpiCardsAsync()
    {
        await Page.GotoAsync("/");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Dashboard" })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("kpi-card")).ToHaveCountAsync(4);
    }

    [Fact]
    public async Task Dashboard_ShowsRecentOrdersAsync()
    {
        await Page.GotoAsync("/");

        await Expect(Page.GetByTestId("order-row")).ToHaveCountAsync(10);
    }

    [Fact]
    public async Task Dashboard_NavigatesToOrdersAsync()
    {
        await Page.GotoAsync("/");

        await Page.GetByRole(AriaRole.Link, new() { Name = "View all" }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Orders" })).ToBeVisibleAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/orders$"));
    }
}
