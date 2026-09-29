<#
.SYNOPSIS
Demo 5 (a): a custom prompt defined in the app (cat).
Same Workspace/ folder, but the persona is a SystemMessage set in Program.cs (append mode).
The folder's pirate instructions are skipped, so the reply only meows and purrs.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot 'CopilotSDKDemo.CustomPromptDemo\CopilotSDKDemo.CustomPromptDemo.csproj'

& dotnet run --project $project
exit $LASTEXITCODE
