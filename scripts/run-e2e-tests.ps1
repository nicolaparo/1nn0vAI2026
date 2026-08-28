<#
.SYNOPSIS
    Runs the Playwright end-to-end tests inside the official Playwright .NET Docker image.

.DESCRIPTION
    Starts the AdventureWorks SQL Server container and the Blazor app on the host, then runs
    AdventureWorks.E2ETests inside mcr.microsoft.com/playwright/dotnet, which already contains the
    .NET 10 SDK and the pre-installed browsers. The container reaches the app on the host through
    host.docker.internal.

    Docker Desktop must be running.
#>
[CmdletBinding()]
param(
    [int] $AppPort = 5080,
    [int] $SqlPort = 14330,
    [string] $SqlPassword = 'Str0ng!Passw0rd',
    [string] $SqlContainerName = 'adventureworks-e2e-sql',
    [switch] $KeepSqlContainer
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$appProcess = $null
$testExitCode = 1

function Wait-ForUrl {
    param([string] $Url, [int] $TimeoutSeconds)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5 | Out-Null
            return
        }
        catch {
            Start-Sleep -Seconds 2
        }
    }

    throw "Timed out waiting for $Url"
}

docker info 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw 'Docker is not available. Start Docker Desktop and wait until the engine is running.'
}

try {
    $existing = docker ps -aq --filter "name=^/$SqlContainerName$"
    if (-not $existing) {
        Write-Host "Starting SQL Server container '$SqlContainerName'..."
        docker run -d --name $SqlContainerName `
            -e ACCEPT_EULA=Y `
            -e "MSSQL_SA_PASSWORD=$SqlPassword" `
            -p "${SqlPort}:1433" `
            chriseaton/adventureworks:latest | Out-Null
    }
    else {
        Write-Host "Reusing SQL Server container '$SqlContainerName'..."
        docker start $SqlContainerName | Out-Null
    }

    Write-Host 'Waiting for the AdventureWorks database restore to finish...'
    $deadline = (Get-Date).AddMinutes(5)
    while ((Get-Date) -lt $deadline) {
        $logs = docker logs $SqlContainerName 2>&1 | Out-String
        if ($logs -match 'Server is ready\.') { break }
        Start-Sleep -Seconds 5
    }

    $connectionString = "Server=localhost,$SqlPort;Database=AdventureWorks;User Id=sa;Password=$SqlPassword;TrustServerCertificate=True"
    $env:ConnectionStrings__AdventureWorks = $connectionString
    $env:ASPNETCORE_ENVIRONMENT = 'Development'

    Write-Host "Starting the Blazor app on port $AppPort..."
    $appProcess = Start-Process -FilePath 'dotnet' -PassThru -NoNewWindow -ArgumentList @(
        'run'
        '--project'
        (Join-Path $repoRoot 'AdventureWorks.BlazorApp')
        '--'
        '--urls'
        "http://0.0.0.0:$AppPort"
    )

    Wait-ForUrl -Url "http://localhost:$AppPort/" -TimeoutSeconds 180

    Write-Host 'Building the Playwright test image...'
    docker build -f (Join-Path $repoRoot 'AdventureWorks.E2ETests\Dockerfile') -t adventureworks-e2e $repoRoot
    if ($LASTEXITCODE -ne 0) { throw 'Failed to build the Playwright test image.' }

    Write-Host 'Running the end-to-end tests...'
    docker run --rm --ipc=host --init `
        -e "E2E_BASE_URL=http://host.docker.internal:$AppPort" `
        --add-host "host.docker.internal:host-gateway" `
        adventureworks-e2e
    $testExitCode = $LASTEXITCODE
}
finally {
    if ($appProcess -and -not $appProcess.HasExited) {
        Write-Host 'Stopping the Blazor app...'
        Stop-Process -Id $appProcess.Id -Force -ErrorAction SilentlyContinue
    }

    if (-not $KeepSqlContainer) {
        Write-Host "Stopping SQL Server container '$SqlContainerName'..."
        docker stop $SqlContainerName 2>&1 | Out-Null
    }
}

exit $testExitCode
