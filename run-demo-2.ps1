<#
.SYNOPSIS
Demo 2: intercept a DEFAULT Copilot tool from C#.
Shows the three interception points, in firing order: the OnPreToolUse hook (sees every tool call),
OnPermissionRequest (per-kind permission gate; shell is rejected) and a custom 'view' tool with
OverridesBuiltInTool that replaces the built-in one. Put breakpoints on the '>>> BREAKPOINT' lines.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot 'CopilotSDKDemo.PermissionDemo\CopilotSDKDemo.PermissionDemo.csproj'

& dotnet run --project $project
exit $LASTEXITCODE
