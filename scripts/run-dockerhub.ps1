<#
.SYNOPSIS
    Starts (or stops) the Sygnia stack entirely from published Docker Hub images -
    sqlserver, seq, jaeger, presentation, frontend - with no local build step.

    Sygnia.Presentation runs as the `presentation` container here (pulled from Docker Hub,
    listening on http://localhost:8080), same as before. The gateway's App.config targets
    https://localhost:7110 for WinHTTP's HTTP/2 ALPN negotiation, which the container does
    NOT serve (it only exposes cleartext h2c on :8080) - so this script additionally starts
    a second, native copy of Sygnia.Presentation (`dotnet run --launch-profile https`,
    :7110/:5058) purely to give Sygnia.Wcf.Gateway a TLS endpoint to call, then starts
    Sygnia.Wcf.Gateway and Sygnia.WpfClient natively, same as run-local.ps1. Ports don't
    collide (container :8080, native :7110/:5058), so both copies of Presentation run side
    by side.

.USAGE
    pwsh scripts/run-dockerhub.ps1                          # pull + start everything
    pwsh scripts/run-dockerhub.ps1 -DockerHubUser someuser   # override the image namespace
    pwsh scripts/run-dockerhub.ps1 -Stop                     # stop everything
#>
param(
    [string]$DockerHubUser = "9032",
    [switch]$Stop
)

$ErrorActionPreference = "Stop"
# PowerShell 7.3+ otherwise turns a native command's non-zero exit code into a terminating
# error honouring $ErrorActionPreference, regardless of output redirection - which would abort
# the readiness retry loops below on their very first (expected) failed attempt.
if ($PSVersionTable.PSVersion -ge [version]"7.3") {
    $PSNativeCommandUseErrorActionPreference = $false
}
Set-Location (Split-Path -Parent $PSScriptRoot)

$env:DOCKERHUB_USER = $DockerHubUser
$sqlPassword = '@1Mops4moa'
$backendRoot = "src/Sygnia.Backend"
$scriptsDir  = "scripts"

if ($Stop) {
    Write-Host "Stopping native processes..."
    Get-Process -Name "Sygnia.Presentation","Sygnia.Wcf.Gateway","Sygnia.WpfClient" -ErrorAction SilentlyContinue |
        ForEach-Object {
            try { $_ | Stop-Process -Force -ErrorAction Stop }
            catch { Write-Warning "Could not stop $($_.ProcessName) (PID $($_.Id)): $($_.Exception.Message)" }
        }

    Write-Host "Bringing docker compose stack down..."
    docker compose down
    Write-Host "Stack stopped."
    exit 0
}

Write-Host "Pulling images from Docker Hub (namespace: $DockerHubUser)..."
# --no-build (implicit here since nothing is invoked with build): pull the tagged images
# instead of rebuilding presentation/frontend from their Dockerfiles.
docker compose pull sqlserver seq jaeger presentation frontend

Write-Host "Starting full stack from pulled images..."
docker compose up -d --no-build sqlserver seq jaeger presentation frontend

# Wait for SQL Server to accept connections before applying schema/seed
Write-Host "Waiting for SQL Server to become ready..."
$maxAttempts = 30
$ready = $false
for ($i = 0; $i -lt $maxAttempts; $i++) {
    docker exec sygnia-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $sqlPassword -C -Q "SELECT 1" *> $null
    if ($LASTEXITCODE -eq 0) {
        $ready = $true
        break
    }
    Start-Sleep -Seconds 2
}
if (-not $ready) {
    throw "SQL Server did not become ready in time."
}
Write-Host "SQL Server is ready."

# Ensure the database exists (the schema/seed scripts assume `sygnia_cash` is already there)
Write-Host "Ensuring sygnia_cash database exists..."
docker exec sygnia-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $sqlPassword -C -Q `
    "IF DB_ID('sygnia_cash') IS NULL CREATE DATABASE sygnia_cash;"
if ($LASTEXITCODE -ne 0) { throw "Failed to ensure sygnia_cash database exists." }

# On a persisted volume, an existing sygnia_cash database can still be finishing recovery for a
# moment after `master` starts accepting connections - wait until it reports ONLINE.
$dbOnline = $false
for ($i = 0; $i -lt $maxAttempts; $i++) {
    $state = docker exec sygnia-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $sqlPassword -C -h -1 -Q `
        "SET NOCOUNT ON; SELECT state_desc FROM sys.databases WHERE name = 'sygnia_cash';"
    if ($LASTEXITCODE -eq 0 -and $state -match "ONLINE") {
        $dbOnline = $true
        break
    }
    Start-Sleep -Seconds 2
}
if (-not $dbOnline) {
    throw "sygnia_cash database did not come online in time."
}

# Apply schema + seed scripts - same idempotent .sql files run-local.ps1 uses.
$sqlFiles = @(
    "00_create_schema.sql",
    "01_seed_accounts.sql",
    "02_seed_users.sql",
    "03_seed_statement_50000.sql"
)
foreach ($file in $sqlFiles) {
    Write-Host "Applying $file..."
    $containerPath = "/tmp/$file"
    docker cp "$scriptsDir/$file" "sygnia-sqlserver-1:$containerPath"
    $applied = $false
    for ($attempt = 0; $attempt -lt 5; $attempt++) {
        docker exec sygnia-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $sqlPassword -C -d sygnia_cash -i $containerPath
        if ($LASTEXITCODE -eq 0) { $applied = $true; break }
        Start-Sleep -Seconds 3
    }
    if (-not $applied) { throw "Failed applying $file" }
}
Write-Host "Schema and seed data applied."

function Wait-ForPort {
    param([int]$Port, [int]$TimeoutSeconds = 60)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $connection = Test-NetConnection -ComputerName "localhost" -Port $Port -WarningAction SilentlyContinue
        if ($connection.TcpTestSucceeded) { return $true }
        Start-Sleep -Seconds 2
    }
    return $false
}

Write-Host "Starting native Sygnia.Presentation (TLS endpoint for the WCF gateway)..."
# launchSettings.json's "https" profile listens on both https://localhost:7110 and
# http://localhost:5058 - the gateway needs :7110 for WinHTTP's HTTP/2 ALPN negotiation,
# which the container's cleartext :8080 endpoint can't provide.
Start-Process -FilePath "dotnet" -ArgumentList "run --project $backendRoot/src/Sygnia.Presentation --launch-profile https" -PassThru | Out-Null

# A cold `dotnet run` build can take well over a minute; starting the gateway before
# Presentation is actually listening on :7110 fails with WinHTTP error 12029
# ("A connection with the server could not be established").
Write-Host "Waiting for native Sygnia.Presentation to become ready on :7110..."
if (-not (Wait-ForPort -Port 7110 -TimeoutSeconds 120)) {
    throw "Sygnia.Presentation did not start listening on :7110 in time."
}
Write-Host "Sygnia.Presentation is ready."

Write-Host "Starting Sygnia.Wcf.Gateway..."
Start-Process -FilePath "dotnet" -ArgumentList "run --project $backendRoot/src/Sygnia.Wcf.Gateway" -PassThru | Out-Null

Write-Host "Starting Sygnia.WpfClient..."
Start-Process -FilePath "dotnet" -ArgumentList "run --project src/Sygnia.WpfClient" -PassThru | Out-Null

Write-Host ""
Write-Host "Stack is up (all from Docker Hub images, namespace '$DockerHubUser'):"
Write-Host "  - sqlserver, seq, jaeger"
Write-Host "  - presentation container (http://localhost:8080)"
Write-Host "  - frontend (http://localhost:4200)"
Write-Host "  - native: Sygnia.Presentation (https://localhost:7110, http://localhost:5058), Sygnia.Wcf.Gateway, Sygnia.WpfClient"
Write-Host "Run with -Stop to shut everything down."
