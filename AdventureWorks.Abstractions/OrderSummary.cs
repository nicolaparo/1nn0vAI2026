namespace AdventureWorks.Abstractions;

public record OrderSummary
{
    public required int SalesOrderID { get; init; }
    public required DateTime OrderDate { get; init; }
    public required byte Status { get; init; }
    public required decimal TotalDue { get; init; }
    public required int CustomerID { get; init; }
    public string? CustomerName { get; init; }

    public string StatusText => Status switch
    {
        1 => "In process",
        2 => "Approved",
        3 => "Backordered",
        4 => "Rejected",
        5 => "Shipped",
        6 => "Cancelled",
        _ => "Unknown",
    };
}
