<#
.SYNOPSIS
    Starts (or stops) the full Sygnia local dev stack: the Docker Compose services
    (sqlserver, seq, jaeger, frontend) plus the native Windows pieces
    (Sygnia.Presentation, Sygnia.Wcf.Gateway, Sygnia.WpfClient).

    Sygnia.Presentation runs NATIVELY here, not via its docker-compose container: the WCF
    gateway's App.config points at https://localhost:7110 (TLS - required for WinHTTP's HTTP/2
    ALPN negotiation; there's no h2c client fix on that side) and the frontend build's baked-in
    API base is http://localhost:5058, both from Presentation's launchSettings.json. Neither
    matches the containerized image's cleartext h2c on :8080, so the container is skipped here
    and the native `dotnet run` is used instead. The `presentation` compose service itself is
    untouched in docker-compose.yml - a plain `docker compose up -d` still brings up all 5 for
    anyone who doesn't need the WCF gateway/WPF client.

.USAGE
    pwsh scripts/run-local.ps1          # start everything
    pwsh scripts/run-local.ps1 -Stop    # stop everything
#>
param(
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

$sqlPassword = '@1Mops4moa'
$backendRoot = "src/Sygnia.Backend"
$scriptsDir  = "scripts"

if ($Stop) {
    Write-Host "Stopping native processes..."
    Get-Process -Name "Sygnia.Presentation","Sygnia.Wcf.Gateway","Sygnia.WpfClient" -ErrorAction SilentlyContinue |
        # Presentation runs natively (see synopsis) so it needs killing here too - `docker
        # compose down` below does not touch it.
        ForEach-Object {
            try { $_ | Stop-Process -Force -ErrorAction Stop }
            catch { Write-Warning "Could not stop $($_.ProcessName) (PID $($_.Id)): $($_.Exception.Message)" }
        }

    Write-Host "Bringing docker compose stack down..."
    # This intentionally tears down the FULL compose project (all 5 services), not just the
    # subset (`--no-deps sqlserver seq jaeger frontend`) that the start path above brings up -
    # e.g. it will also stop a `presentation` container if one happens to be running because a
    # user separately ran a plain `docker compose up -d`.
    docker compose down

    Write-Host "Stack stopped."
    exit 0
}

Write-Host "Starting docker compose services (sqlserver, seq, jaeger, frontend)..."
# `presentation` is deliberately excluded - Sygnia.Presentation runs natively below instead
# (see script synopsis for why: TLS/port mismatch with the Wcf.Gateway and frontend clients).
# --no-deps: `frontend` declares `depends_on: presentation` in docker-compose.yml (left
# unchanged - a plain `docker compose up -d` should still bring up all 5 for other users), so
# without --no-deps compose would silently start the presentation container anyway.
# GRPC_URL overrides docker-compose.yml's default (:8080, for the container) so the frontend's
# runtime env.js points at native Presentation's :5058 instead (see nginx docker-entrypoint.d).
$env:GRPC_URL = "http://localhost:5058"
docker compose up -d --no-deps sqlserver seq jaeger frontend

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

# Apply schema + seed scripts. Real mechanism found in scripts/: raw .sql
# files, each idempotent (guarded by __EFMigrationsHistory / IF NOT EXISTS / row-count checks),
# NOT `dotnet ef database update` - no EF CLI tooling is set up for this repo.
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
    # 00_create_schema.sql has no leading `USE sygnia_cash;`, unlike the seed scripts, so
    # target the database explicitly via -d for every file.
    # Retry a few times: right after container start, sys.databases can report the database
    # ONLINE a moment before the login/connect against it actually succeeds.
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

Write-Host "Starting Sygnia.Presentation..."
# launchSettings.json defines "http" and "https" profiles; `dotnet run` picks the first
# ("http", :5058 only) unless told otherwise. The Wcf.Gateway needs the TLS endpoint too, so
# force the "https" profile, which listens on both https://localhost:7110 and http://localhost:5058.
Start-Process -FilePath "dotnet" -ArgumentList "run --project $backendRoot/src/Sygnia.Presentation --launch-profile https" -PassThru | Out-Null

# A cold `dotnet run` build can take well over a minute; starting the gateway before
# Presentation is actually listening on :7110 fails with WinHTTP error 12029
# ("A connection with the server could not be established").
Write-Host "Waiting for Sygnia.Presentation to become ready on :7110..."
if (-not (Wait-ForPort -Port 7110 -TimeoutSeconds 120)) {
    throw "Sygnia.Presentation did not start listening on :7110 in time."
}
Write-Host "Sygnia.Presentation is ready."

Write-Host "Starting Sygnia.Wcf.Gateway..."
Start-Process -FilePath "dotnet" -ArgumentList "run --project $backendRoot/src/Sygnia.Wcf.Gateway" -PassThru | Out-Null

Write-Host "Starting Sygnia.WpfClient..."
Start-Process -FilePath "dotnet" -ArgumentList "run --project src/Sygnia.WpfClient" -PassThru | Out-Null

Write-Host ""
Write-Host "Stack is up:"
Write-Host "  - docker compose: sqlserver, seq, jaeger, frontend (localhost:4200)"
Write-Host "  - native: Sygnia.Presentation (https://localhost:7110, http://localhost:5058), Sygnia.Wcf.Gateway, Sygnia.WpfClient"
Write-Host "Run with -Stop to shut everything down."
