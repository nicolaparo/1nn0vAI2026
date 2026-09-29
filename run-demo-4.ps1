<#
.SYNOPSIS
Demo 4: the Workspace/ folder's instructions are honored by the SDK.
The session's WorkingDirectory is Workspace/. The C# code has no persona: the pirate replies come
from Workspace/.github/copilot-instructions.md, which Copilot loads from the working directory.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot 'CopilotSDKDemo.InstructionsDemo\CopilotSDKDemo.InstructionsDemo.csproj'

& dotnet run --project $project
exit $LASTEXITCODE
