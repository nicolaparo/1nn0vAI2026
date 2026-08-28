---
name: dotnet-cli
description: Use the .NET CLI for creating, wiring, building, testing, running, formatting, and publishing .NET 10/C# projects instead of manually creating generated files.
---

# .NET CLI Skill

Use this skill when creating or modifying .NET solutions, projects, package references, project references, builds, tests, runs, publishing, or formatting.

## Principle

Prefer the official `dotnet` CLI over manual file creation or hand-editing generated project structure. Manual edits are acceptable only when the CLI cannot express the required change or when carefully preserving existing custom MSBuild configuration.

## Discovery

1. Inspect the repository for existing `.sln`, `.slnx`, `.csproj`, `Directory.Build.props`, `Directory.Build.targets`, and `global.json` files.
2. Use `dotnet --info` or `dotnet --version` when SDK availability or target framework compatibility matters.
3. Use `dotnet new list` before choosing templates if the template name or installed workload is uncertain.

## Create and Wire Projects

Use these commands as the default patterns:

```powershell
dotnet new sln -n MySolution
dotnet new webapi -n MyApp.Api -f net10.0
dotnet new blazor -n MyApp.Web -f net10.0
dotnet new classlib -n MyApp.Core -f net10.0
dotnet new xunit -n MyApp.Tests -f net10.0
dotnet sln MySolution.sln add MyApp.Api\MyApp.Api.csproj MyApp.Web\MyApp.Web.csproj MyApp.Core\MyApp.Core.csproj MyApp.Tests\MyApp.Tests.csproj
dotnet add MyApp.Api\MyApp.Api.csproj reference MyApp.Core\MyApp.Core.csproj
dotnet add MyApp.Tests\MyApp.Tests.csproj reference MyApp.Core\MyApp.Core.csproj
```

## Manage Dependencies

Use CLI commands for dependencies and references:

```powershell
dotnet add MyApp.Api\MyApp.Api.csproj package Microsoft.EntityFrameworkCore
dotnet remove MyApp.Api\MyApp.Api.csproj package Microsoft.EntityFrameworkCore
dotnet add MyApp.Api\MyApp.Api.csproj reference MyApp.Core\MyApp.Core.csproj
dotnet remove MyApp.Api\MyApp.Api.csproj reference MyApp.Core\MyApp.Core.csproj
dotnet list MyApp.Api\MyApp.Api.csproj package
dotnet list MyApp.Api\MyApp.Api.csproj reference
```

## Build, Test, Run, and Publish

Prefer targeted commands:

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet run --project MyApp.Api\MyApp.Api.csproj
dotnet watch --project MyApp.Web\MyApp.Web.csproj
dotnet publish MyApp.Api\MyApp.Api.csproj -c Release
dotnet format
```

## Rules

- Do not manually create application/project skeletons when `dotnet new` can create them.
- Do not manually add package references when `dotnet add package` can add them.
- Do not manually add project references when `dotnet add reference` can add them.
- Do not manually update solution membership when `dotnet sln add` or `dotnet sln remove` can do it.
- Validate structural changes with the smallest relevant `dotnet restore`, `dotnet build`, or `dotnet test` command.
- Keep commands Windows-friendly in this repository by using backslashes in paths.
