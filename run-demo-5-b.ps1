<#
.SYNOPSIS
Demo 5 (b): custom prompt AND folder instructions together.
Same as demo 5 (a) but the folder's copilot-instructions.md is loaded too (--with-folder), so the
app's cat prompt and the folder's pirate instructions are both applied and the reply mixes them.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot 'CopilotSDKDemo.CustomPromptDemo\CopilotSDKDemo.CustomPromptDemo.csproj'

& dotnet run --project $project -- --with-folder
exit $LASTEXITCODE
