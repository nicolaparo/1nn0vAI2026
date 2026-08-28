---
applyTo: "**/*.sln,**/*.slnx,**/*.csproj,**/*.props,**/*.targets,**/*.cs,**/*.razor"
---

# .NET CLI Instructions

Use the `dotnet` CLI as the default way to create and maintain .NET 10 projects. Prefer official CLI commands over manually creating or editing generated files.

## Default Workflow

- Inspect existing solution and project structure before adding anything.
- Use `dotnet --info` or `dotnet --version` when SDK compatibility matters.
- Use `dotnet new list` to discover available templates instead of guessing template names.
- Use `dotnet new <template>` to create solutions, projects, configuration files, and common scaffolding.
- Use `dotnet sln` to add, remove, or list projects in a solution.
- Use `dotnet add package` and `dotnet remove package` for NuGet package changes.
- Use `dotnet add reference` and `dotnet remove reference` for project references.
- Use `dotnet restore`, `dotnet build`, `dotnet test`, `dotnet run`, `dotnet watch`, `dotnet publish`, and `dotnet format` when those commands match the task.

## Creation Rules

- Create applications with `dotnet new`, not by manually writing project files and starter source files.
- Create class libraries, test projects, Blazor apps, Razor components projects, worker services, Web APIs, and console apps from templates.
- Add new projects to the solution immediately after creation with `dotnet sln add`.
- Add references between projects with `dotnet add <project> reference <referenced-project>`.
- Add NuGet dependencies with `dotnet add <project> package <package-name>`.
- Only manually edit `.csproj`, `.sln`, `.slnx`, `.props`, or `.targets` files when the CLI cannot express the required change or when preserving existing custom MSBuild structure requires manual care.

## Common Commands

```powershell
dotnet new sln -n MySolution
dotnet new webapi -n MyApp.Api -f net10.0
dotnet new blazor -n MyApp.Web -f net10.0
dotnet new classlib -n MyApp.Core -f net10.0
dotnet new xunit -n MyApp.Tests -f net10.0
dotnet sln MySolution.sln add MyApp.Api\MyApp.Api.csproj MyApp.Core\MyApp.Core.csproj MyApp.Tests\MyApp.Tests.csproj
dotnet add MyApp.Api\MyApp.Api.csproj reference MyApp.Core\MyApp.Core.csproj
dotnet add MyApp.Tests\MyApp.Tests.csproj reference MyApp.Core\MyApp.Core.csproj
dotnet add MyApp.Api\MyApp.Api.csproj package Microsoft.EntityFrameworkCore
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet run --project MyApp.Api\MyApp.Api.csproj
dotnet publish MyApp.Api\MyApp.Api.csproj -c Release
```

## Validation

- After changing project structure, run the smallest relevant `dotnet restore`, `dotnet build`, or `dotnet test` command.
- Prefer targeted validation first, then widen scope only when necessary.
- Do not install new tools unless the repository already uses them or the requested task requires them.
