[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$appHostProject = Join-Path $PSScriptRoot 'AdventureWorks.AppHost\AdventureWorks.AppHost.csproj'

& dotnet run --project $appHostProject
exit $LASTEXITCODE
