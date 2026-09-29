<#
.SYNOPSIS
Demo 3 (b): sandbox OFF, for contrast with demo 3 (a).
Same scenario without the sandbox, so the approved attacks succeed. Only touches temp files.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot 'CopilotSDKDemo.SandboxDemo\CopilotSDKDemo.SandboxDemo.csproj'

& dotnet run --project $project -- --no-sandbox
exit $LASTEXITCODE
