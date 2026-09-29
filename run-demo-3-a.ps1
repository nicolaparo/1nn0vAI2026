<#
.SYNOPSIS
Demo 3 (a): sandbox ON.
The permission handler approves everything on purpose; the Copilot sandbox is the second gate.
It limits an approved command to the workspace, denies a protected folder and blocks the network,
so the attacks fail and the protected file stays intact.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot 'CopilotSDKDemo.SandboxDemo\CopilotSDKDemo.SandboxDemo.csproj'

& dotnet run --project $project
exit $LASTEXITCODE
