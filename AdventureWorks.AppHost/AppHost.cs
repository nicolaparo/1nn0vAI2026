var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddSqlServer("sql")
    .WithImageRegistry("docker.io")
    .WithImage("chriseaton/adventureworks")
    .WithImageTag("latest")
    .WithLifetime(ContainerLifetime.Persistent);

// The chriseaton/adventureworks image restores the AdventureWorks database itself, so instead of
// creating it, wait until the restore performed by the container entry point has completed.
var waitForRestoreScript = """
    DECLARE @attempt int = 0;
    WHILE NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'AdventureWorks') AND @attempt < 120
    BEGIN
        WAITFOR DELAY '00:00:05';
        SET @attempt = @attempt + 1;
    END
    """;

var adventureWorks = sqlServer.AddDatabase("AdventureWorks")
    .WithCreationScript(waitForRestoreScript);

builder.AddProject<Projects.AdventureWorks_BlazorApp>("blazorapp")
    .WithReference(adventureWorks)
    .WaitFor(adventureWorks)
    .WithExternalHttpEndpoints();

builder.Build().Run();
