---
applyTo: "**/*.cs,**/*.csproj,**/*.sln,**/*.slnx,**/Program.cs,**/*.json"
---

# .NET Aspire Instructions

Use .NET Aspire for cloud-ready, observable, distributed .NET applications when the solution has multiple services, backing resources, or local orchestration needs. Keep Aspire usage simple and explicit.

## When to Use Aspire

- Use Aspire for multi-project applications that need local orchestration, service discovery, health checks, telemetry, dashboards, containers, or backing services such as Redis, PostgreSQL, SQL Server, queues, storage, or APIs.
- Do not introduce Aspire for a single simple project unless the user explicitly asks for it.
- Prefer Aspire's AppHost and ServiceDefaults patterns over custom local orchestration scripts.
- Prefer official Aspire integrations over hand-written connection string and container wiring.

## Project Structure

- Use an `.AppHost` project as the orchestration entry point.
- Use a `.ServiceDefaults` project for shared service configuration such as OpenTelemetry, health checks, service discovery, resilience, logging, and default endpoints.
- Reference `.ServiceDefaults` from each participating service and call `builder.AddServiceDefaults()` during startup.
- Call `app.MapDefaultEndpoints()` in services when the project uses Aspire health and default endpoints.
- Keep application services independent from AppHost-only orchestration concerns.

## CLI First

- Prefer `dotnet` and Aspire CLI commands over manual project scaffolding.
- Use `dotnet new aspire-starter` or `dotnet new aspire-empty` for new Aspire solutions.
- Use AppHost and ServiceDefaults templates when adding Aspire to an existing solution.
- Run Aspire apps through the AppHost project with `dotnet run --project <AppName>.AppHost\<AppName>.AppHost.csproj` unless the repository uses `aspire run`.
- Use `dotnet add reference` and `dotnet add package` for wiring projects and Aspire integrations.

## AppHost Orchestration

- Declare the distributed application topology in AppHost with `DistributedApplication.CreateBuilder(args)`.
- Use clear resource names because they become service discovery names, dashboard labels, and configuration keys.
- Express dependencies with `.WithReference(...)` and startup ordering with `.WaitFor(...)` when needed.
- Prefer service discovery references over hard-coded URLs and ports.
- Keep AppHost code declarative. Avoid putting business logic in AppHost.
- Use persistent containers or volumes only when the development workflow requires persisted state.

## Integrations and Configuration

- Prefer Aspire hosting integrations such as Redis, PostgreSQL, SQL Server, storage, queues, and messaging when available.
- Keep secrets out of source control. Use user secrets, environment variables, or the platform secret store.
- Do not duplicate connection strings across projects. Let Aspire inject configuration through references whenever possible.
- Configure telemetry, health checks, and resiliency once in ServiceDefaults rather than repeating it in every service.

## Dashboard and Observability

- Use the Aspire dashboard during local development to inspect logs, traces, metrics, health, configuration, and service dependencies.
- Prefer structured logging and standard OpenTelemetry instrumentation.
- Do not add custom logging dashboards or diagnostics plumbing when the Aspire dashboard and OpenTelemetry already cover the need.

## Deployment

- Treat AppHost as the source of application topology for deployment planning.
- Keep development orchestration separate from production runtime assumptions.
- Use Aspire-supported deployment paths and generated manifests when appropriate, but keep deployment configuration explicit and reviewable.
