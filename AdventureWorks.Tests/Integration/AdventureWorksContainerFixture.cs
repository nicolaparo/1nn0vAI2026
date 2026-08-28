using AdventureWorks.BlazorApp.Data;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;

namespace AdventureWorks.Tests.Integration;

/// <summary>
/// Starts the AdventureWorks SQL Server image once per test collection. The image restores the
/// database from a backup on start up, so the readiness probe waits for the entry point log message
/// instead of the plain SQL Server readiness check.
/// </summary>
public sealed class AdventureWorksContainerFixture : IAsyncLifetime
{
    private const string SaPassword = "Str0ng!Passw0rd";

    private readonly IContainer container = new ContainerBuilder("chriseaton/adventureworks:latest")
        .WithEnvironment("ACCEPT_EULA", "Y")
        .WithEnvironment("MSSQL_SA_PASSWORD", SaPassword)
        .WithPortBinding(1433, assignRandomHostPort: true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Server is ready."))
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await container.StartAsync();

        ConnectionString =
            $"Server={container.Hostname},{container.GetMappedPublicPort(1433)};" +
            $"Database=AdventureWorks;User Id=sa;Password={SaPassword};TrustServerCertificate=True";
    }

    public Task DisposeAsync() => container.DisposeAsync().AsTask();

    public AdventureWorksDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AdventureWorksDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new AdventureWorksDbContext(options);
    }
}

[CollectionDefinition(Name)]
public sealed class AdventureWorksCollection : ICollectionFixture<AdventureWorksContainerFixture>
{
    public const string Name = "AdventureWorks database";
}
