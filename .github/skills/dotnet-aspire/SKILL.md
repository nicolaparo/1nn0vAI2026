---
name: dotnet-aspire
description: Use .NET Aspire for cloud-ready distributed .NET applications with AppHost orchestration, ServiceDefaults, integrations, dashboard observability, and CLI-first workflows.
---

# .NET Aspire Skill

Use this skill when the task involves .NET Aspire, distributed .NET applications, AppHost, ServiceDefaults, service discovery, local orchestration, Aspire integrations, dashboard observability, or cloud-ready .NET app composition.

## Core Approach

Build Aspire solutions with the standard AppHost and ServiceDefaults model:

- `.AppHost` defines and runs the distributed application topology.
- `.ServiceDefaults` centralizes cross-cutting defaults such as OpenTelemetry, health checks, service discovery, resilience, logging, and default endpoints.
- Application projects contain business logic and should not depend on AppHost orchestration details.

Use Aspire when it adds real value. Do not add it to a simple single-project application unless requested.

## Discovery Checklist

1. Look for existing `.sln`, `.slnx`, `.csproj`, `global.json`, `.AppHost`, and `.ServiceDefaults` projects.
2. Check whether services already call `builder.AddServiceDefaults()` and `app.MapDefaultEndpoints()`.
3. Inspect AppHost `Program.cs` for existing resources, references, and naming conventions.
4. Use `dotnet --info`, `dotnet --version`, and `dotnet new list` when SDK or template availability matters.

## CLI-First Workflow

Prefer commands over manual scaffolding:

```powershell
dotnet new aspire-starter -n MyApp
dotnet new aspire-empty -n MyApp
dotnet new aspire-apphost -n MyApp.AppHost
dotnet new aspire-servicedefaults -n MyApp.ServiceDefaults
dotnet sln MyApp.sln add MyApp.AppHost\MyApp.AppHost.csproj MyApp.ServiceDefaults\MyApp.ServiceDefaults.csproj
dotnet add MyApp.Api\MyApp.Api.csproj reference MyApp.ServiceDefaults\MyApp.ServiceDefaults.csproj
dotnet add MyApp.AppHost\MyApp.AppHost.csproj reference MyApp.Api\MyApp.Api.csproj
dotnet run --project MyApp.AppHost\MyApp.AppHost.csproj
```

Use the Aspire CLI if the repository already uses it or the user asks for it:

```powershell
aspire new
aspire run
```

## AppHost Rules

- Use `DistributedApplication.CreateBuilder(args)` in AppHost.
- Add projects with `builder.AddProject<Projects.ProjectName>("service-name")`.
- Add backing resources with official Aspire hosting integrations when available.
- Use `.WithReference(...)` to provide service discovery, connection strings, and configuration to dependent projects.
- Use `.WaitFor(...)` only when startup ordering is required.
- Keep resource names short, stable, lowercase where practical, and meaningful.
- Keep AppHost declarative. Do not put business logic, data access, or application workflows in AppHost.

Example:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var api = builder.AddProject<Projects.MyApp_Api>("api")
    .WithReference(cache);

builder.AddProject<Projects.MyApp_Web>("web")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
```

## ServiceDefaults Rules

- Reference ServiceDefaults from every service that participates in Aspire orchestration.
- Call `builder.AddServiceDefaults()` in each service startup.
- Call `app.MapDefaultEndpoints()` when health/default endpoints are part of the app pattern.
- Centralize OpenTelemetry, health checks, service discovery, logging, and resilience in ServiceDefaults.
- Do not copy the same telemetry, health, and resilience setup into every service.

Example:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var app = builder.Build();

app.MapDefaultEndpoints();

app.Run();
```

## Integrations

- Prefer official Aspire integrations for Redis, PostgreSQL, SQL Server, Azure resources, queues, storage, messaging, and containers.
- Add packages with `dotnet add package`.
- Avoid hard-coded ports, URLs, and connection strings. Let Aspire references inject configuration whenever possible.
- Keep secrets out of source control. Use user secrets, environment variables, or platform secret stores.

## Dashboard and Observability

- Use the Aspire dashboard for local inspection of resources, logs, traces, metrics, health, configuration, and dependencies.
- Prefer structured logs and standard OpenTelemetry instrumentation.
- Do not build custom local dashboards or ad hoc diagnostics when Aspire's dashboard provides the required visibility.

## Validation

- After Aspire changes, run the smallest relevant command:

```powershell
dotnet restore
dotnet build --no-restore
dotnet run --project MyApp.AppHost\MyApp.AppHost.csproj
```

- If the app starts, use the Aspire dashboard to confirm services and resources are visible and healthy when practical.
