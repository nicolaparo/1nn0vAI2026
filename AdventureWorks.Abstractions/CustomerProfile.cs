namespace AdventureWorks.Abstractions;

public record CustomerProfile
{
    public required int CustomerID { get; init; }
    public string? CompanyName { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? EmailAddress { get; init; }
    public string? Phone { get; init; }

    public string DisplayName => string.IsNullOrWhiteSpace(CompanyName)
        ? $"{FirstName} {LastName}".Trim()
        : CompanyName;

    public string ContactName => $"{FirstName} {LastName}".Trim();
}
