<#
.SYNOPSIS
    Starts (or stops) the full Sygnia local dev stack: the Docker Compose services
    (sqlserver, seq, jaeger, presentation, frontend) plus the two Windows-only
    pieces that can't be containerized (Sygnia.Wcf.Gateway, Sygnia.WpfClient).

.USAGE
    pwsh scripts/run-local.ps1          # start everything
    pwsh scripts/run-local.ps1 -Stop    # stop everything
#>
param(
    [switch]$Stop
)

$ErrorActionPreference = "Stop"
Set-Location (Split-Path -Parent $PSScriptRoot)

$sqlPassword = '@1Mops4moa'
$backendRoot = "src/Sygnia.Backend"
$scriptsDir  = "$backendRoot/scripts"

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

Write-Host "Starting full docker compose stack (sqlserver, seq, jaeger, presentation, frontend)..."
docker compose up -d

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
# moment after `master` starts accepting connections — wait until it reports ONLINE.
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

# Apply schema + seed scripts. Real mechanism found in src/Sygnia.Backend/scripts: raw .sql
# files, each idempotent (guarded by __EFMigrationsHistory / IF NOT EXISTS / row-count checks),
# NOT `dotnet ef database update` — no EF CLI tooling is set up for this repo.
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

Write-Host "Starting Sygnia.Wcf.Gateway..."
Start-Process -FilePath "dotnet" -ArgumentList "run --project $backendRoot/src/Sygnia.Wcf.Gateway" -PassThru | Out-Null

Write-Host "Starting Sygnia.WpfClient..."
Start-Process -FilePath "dotnet" -ArgumentList "run --project src/Sygnia.WpfClient" -PassThru | Out-Null

Write-Host ""
Write-Host "Stack is up:"
Write-Host "  - docker compose: sqlserver, seq, jaeger, presentation (localhost:8080), frontend (localhost:4200)"
Write-Host "  - native: Sygnia.Wcf.Gateway, Sygnia.WpfClient"
Write-Host "Run with -Stop to shut everything down."
