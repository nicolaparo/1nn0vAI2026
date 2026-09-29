<#
.SYNOPSIS
Demo 1: the AdventureWorks app.
Starts the Aspire AppHost, which runs the AdventureWorks Blazor app (and its services) that
the Copilot SDK demos build on.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$appHostProject = Join-Path $PSScriptRoot 'AdventureWorks.AppHost\AdventureWorks.AppHost.csproj'

& dotnet run --project $appHostProject
exit $LASTEXITCODE
