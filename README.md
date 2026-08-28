# 1nn0vAI2026

AdventureWorks ERP/CRM sample: a .NET 10 Blazor Web App orchestrated with .NET Aspire on top of the
AdventureWorks SQL Server database.

## Projects

| Project | Description |
| --- | --- |
| `AdventureWorks.AppHost` | Aspire orchestration. Runs SQL Server from the `chriseaton/adventureworks:latest` image and passes the `AdventureWorks` database reference to the Blazor app. |
| `AdventureWorks.ServiceDefaults` | Shared telemetry, health checks, resilience, and service discovery. |
| `AdventureWorks.BlazorApp` | Blazor Web App (Interactive Server) with the EF Core model, data services, and UI. |

## Prerequisites

- .NET 10 SDK
- **Docker Desktop must be installed *and running* before starting the AppHost.**
  Aspire starts the SQL Server container through the Docker API, so if Docker Desktop is not running
  you will get an error such as:

  ```text
  failed to connect to the docker API at npipe:////./pipe/dockerDesktopLinuxEngine;
  check if the path is correct and if the daemon is running
  ```

  Start Docker Desktop and wait until it reports *Engine running*, then verify with:

  ```powershell
  docker info
  ```

## Running

```powershell
dotnet run --project AdventureWorks.AppHost\AdventureWorks.AppHost.csproj
```

The first run pulls the `chriseaton/adventureworks` image and restores the database inside the
container, which takes a few minutes. The Aspire dashboard shows the container and the Blazor app
endpoints.

The container is registered with a persistent lifetime, so the restored database survives restarts.
