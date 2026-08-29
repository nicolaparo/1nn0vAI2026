using AdventureWorks.Abstractions;
using Shouldly;

namespace AdventureWorks.Tests;

public class OrderSummaryTests
{
    [Theory]
    [InlineData((byte)1, "In process")]
    [InlineData((byte)2, "Approved")]
    [InlineData((byte)3, "Backordered")]
    [InlineData((byte)4, "Rejected")]
    [InlineData((byte)5, "Shipped")]
    [InlineData((byte)6, "Cancelled")]
    [InlineData((byte)0, "Unknown")]
    [InlineData((byte)9, "Unknown")]
    public void StatusText_MapsDocumentedStatusCodes(byte status, string expected)
    {
        var order = CreateOrder(status);

        order.StatusText.ShouldBe(expected);
    }

    private static OrderSummary CreateOrder(byte status) => new()
    {
        SalesOrderID = 1,
        OrderDate = new DateTime(2024, 1, 1),
        Status = status,
        TotalDue = 100m,
        CustomerID = 1,
    };
}
