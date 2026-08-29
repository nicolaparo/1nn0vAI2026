using Microsoft.Playwright;

namespace AdventureWorks.E2ETests;

public class OrdersTests : AdventureWorksPageTest
{
    [Fact]
    public async Task Orders_ListsOrdersAsync()
    {
        await Page.GotoAsync("/orders");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Orders" })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("order-row")).ToHaveCountAsync(10);
    }

    [Fact]
    public async Task Orders_PageSizeChangeReloadsTheGridAsync()
    {
        await Page.GotoAsync("/orders");
        await Expect(Page.GetByTestId("order-row")).ToHaveCountAsync(10);

        await Page.GetByLabel("Rows per page").SelectOptionAsync("50");

        await Expect(Page.GetByTestId("order-row")).ToHaveCountAsync(50);
    }

    [Fact]
    public async Task Orders_LinkThroughToCustomerDetailAsync()
    {
        await Page.GotoAsync("/orders");
        await Expect(Page.GetByTestId("order-row").First).ToBeVisibleAsync();

        await Page.GetByTestId("order-row").First.GetByRole(AriaRole.Link).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex(@"/customers/\d+$"));
        await Expect(Page.GetByText("Profile")).ToBeVisibleAsync();
    }
}
