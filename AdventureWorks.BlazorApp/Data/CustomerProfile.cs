namespace AdventureWorks.BlazorApp.Data;

/// <summary>
/// Flattened customer view. In the AdventureWorks OLTP schema a customer is either a store or an
/// individual, and contact data lives in the Person tables, so the UI-facing shape is a projection.
/// </summary>
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
