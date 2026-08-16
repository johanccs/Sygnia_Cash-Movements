# Docker Compose Full Stack & Run Script Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend the existing `docker-compose.yml` (currently SQL Server + Seq + Jaeger only) to also build and run `Sygnia.Presentation`, `Sygnia.Frontend`, and `Sygnia.Wcf.Gateway`, publishable to Docker Hub; add a script that seeds the DB (idempotently) and starts/stops `Sygnia.Presentation` + `Sygnia.Wcf.Gateway`.

**Architecture:** One `Dockerfile` per containerizable project (`Sygnia.Presentation`, `Sygnia.Frontend`); `Sygnia.Wcf.Gateway` and `Sygnia.WpfClient` are Windows-only .NET Framework and stay out of docker-compose (matches CLAUDE.md's note that the WCF gateway is Windows-only) — they're covered by the run script instead, run natively.

**Tech Stack:** Docker, docker-compose, PowerShell (repo's primary shell on this machine).

**Spec:** `docs/project-scaffold-done.md` Modifications items 8-9; root `CLAUDE.md` intended command `docker compose up -d`.

## Global Constraints

- Root CLAUDE.md's target command is `docker compose up -d` bringing up "SQL Server + Seq + Jaeger + host + SPA" — this plan fills in "host" (`Sygnia.Presentation`) and "SPA" (`Sygnia.Frontend`).
- `Sygnia.Wcf.Gateway` is Windows-only (.NET Framework 4.8) — do not attempt to containerize it; it and `Sygnia.WpfClient` run natively via the script instead.
- Existing services (`sqlserver`, `seq`, `jaeger`) and their env vars/ports in `docker-compose.yml` must not change.

---

### Task 1: Dockerfile for Sygnia.Presentation

**Files:**
- Create: `src/Sygnia.Backend/src/Sygnia.Presentation/Dockerfile`

**Interfaces:**
- Consumes: `src/Sygnia.Backend/Sygnia.Backend.sln`, `global.json` (SDK 8.0.319), `Directory.Build.props`, `Directory.Packages.props`
- Produces: an image exposing the gRPC port (check `Sygnia.Presentation`'s `appsettings.json`/`Program.cs` for the configured Kestrel port; default to `8080`/`8081` if unconfigured, matching ASP.NET Core 8 container defaults)

- [ ] **Step 1: Write the multi-stage Dockerfile**

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/Sygnia.Backend/ .
RUN dotnet restore Sygnia.Backend.sln
RUN dotnet publish src/Sygnia.Presentation/Sygnia.Presentation.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Sygnia.Presentation.dll"]
```

- [ ] **Step 2: Build the image locally**

Run: `docker build -f src/Sygnia.Backend/src/Sygnia.Presentation/Dockerfile -t sygnia-presentation:local .`
Expected: build succeeds.

- [ ] **Step 3: Smoke-test the container against a running SQL Server**

Run: `docker compose up -d sqlserver` then `docker run --rm -p 8080:8080 --network sygnia_default sygnia-presentation:local` (adjust connection string via env var override) and check logs for a clean startup, no crash.
Expected: process starts and listens; stop it with Ctrl+C once confirmed.

- [ ] **Step 4: Commit**

```bash
git add src/Sygnia.Backend/src/Sygnia.Presentation/Dockerfile
git commit -m "build: add Dockerfile for Sygnia.Presentation"
```

---

### Task 2: Dockerfile for Sygnia.Frontend

**Files:**
- Create: `src/Sygnia.Frontend/Dockerfile`

**Interfaces:**
- Consumes: `src/Sygnia.Frontend/package.json`, `angular.json`
- Produces: an nginx-served static build on port `80`

- [ ] **Step 1: Write the multi-stage Dockerfile**

```dockerfile
FROM node:20 AS build
WORKDIR /app
COPY src/Sygnia.Frontend/package*.json ./
RUN npm ci
COPY src/Sygnia.Frontend/ .
RUN npx ng build --configuration production

FROM nginx:alpine
COPY --from=build /app/dist/sygnia.frontend/browser /usr/share/nginx/html
EXPOSE 80
```

(Adjust the `dist/...` path to match this project's actual `angular.json` `outputPath` — check it before finalizing.)

- [ ] **Step 2: Verify the output path**

Run: `grep -n "outputPath" src/Sygnia.Frontend/angular.json`
Expected: confirms the exact `dist/<project>/browser` path to COPY from; fix the Dockerfile if it differs from the guess above.

- [ ] **Step 3: Build the image locally**

Run: `docker build -f src/Sygnia.Frontend/Dockerfile -t sygnia-frontend:local src/Sygnia.Frontend`
Expected: build succeeds.

- [ ] **Step 4: Commit**

```bash
git add src/Sygnia.Frontend/Dockerfile
git commit -m "build: add Dockerfile for Sygnia.Frontend"
```

---

### Task 3: Extend docker-compose.yml with host, SPA, and Docker Hub-ready image names

**Files:**
- Modify: `docker-compose.yml`

**Interfaces:**
- Consumes: Dockerfiles from Task 1 and Task 2
- Produces: `docker compose up -d` starts all five services

- [ ] **Step 1: Add the `presentation` and `frontend` services**

```yaml
  presentation:
    build:
      context: .
      dockerfile: src/Sygnia.Backend/src/Sygnia.Presentation/Dockerfile
    image: ${DOCKERHUB_USER:-sygnia}/sygnia-presentation:latest
    depends_on:
      - sqlserver
      - seq
      - jaeger
    environment:
      ConnectionStrings__Default: "Server=sqlserver;Database=Sygnia;User Id=sa;Password=@1Mops4moa;TrustServerCertificate=True"
      Seq__ServerUrl: "http://seq:80"
      Otlp__Endpoint: "http://jaeger:4317"
    ports:
      - "8080:8080"

  frontend:
    build:
      context: src/Sygnia.Frontend
      dockerfile: Dockerfile
    image: ${DOCKERHUB_USER:-sygnia}/sygnia-frontend:latest
    depends_on:
      - presentation
    ports:
      - "4200:80"
```

(Confirm actual env var names against `Sygnia.Presentation/appsettings.json` before finalizing — the plan's names are best-guess from `SOLUTION.md`'s references to `Seq:ServerUrl` and `Otlp:Endpoint`.)

- [ ] **Step 2: Full stack smoke test**

Run: `docker compose up -d` then check `docker compose ps` shows all 5 services healthy/running, and `curl http://localhost:4200` returns the Angular index page.
Expected: all containers up; frontend reachable.

- [ ] **Step 3: Tear down**

Run: `docker compose down`

- [ ] **Step 4: Commit**

```bash
git add docker-compose.yml
git commit -m "build: wire Sygnia.Presentation and Sygnia.Frontend into docker-compose"
```

---

### Task 4: Startup/shutdown script for the Windows-only pieces

**Files:**
- Create: `scripts/run-local.ps1`

**Interfaces:**
- Consumes: `docker-compose.yml` (Task 3), a schema/seed script (check `src/Sygnia.Backend/src/Sygnia.Infrastructure` for an existing migration or seed SQL file — if none exists, use `dotnet ef database update`)
- Produces: a single script a developer runs to get `sqlserver`+`seq`+`jaeger` up via Docker, apply migrations/seed if not already applied, then start `Sygnia.Presentation` and `Sygnia.Wcf.Gateway` natively, and a corresponding stop path

- [ ] **Step 1: Locate the actual seed/migration mechanism**

Run: `Get-ChildItem -Recurse -Filter "*.sql" src/Sygnia.Backend` and `grep -rn "Migrations" src/Sygnia.Backend/src/Sygnia.Infrastructure`
Expected: identifies whether seeding is EF migrations, a raw `.sql` script, or `DbContext.Database.EnsureCreated()` — the script must call whichever mechanism actually exists, not a guessed one.

- [ ] **Step 2: Write the script**

```powershell
param(
    [switch]$Stop
)

if ($Stop) {
    Get-Process -Name "Sygnia.Presentation","Sygnia.Wcf.Gateway" -ErrorAction SilentlyContinue | Stop-Process -Force
    docker compose down
    exit 0
}

docker compose up -d sqlserver seq jaeger

# Wait for SQL Server to accept connections before migrating
$maxAttempts = 30
for ($i = 0; $i -lt $maxAttempts; $i++) {
    $result = docker exec sygnia-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '@1Mops4moa' -C -Q "SELECT 1" 2>$null
    if ($LASTEXITCODE -eq 0) { break }
    Start-Sleep -Seconds 2
}

# Apply migrations only if not already applied (idempotent — dotnet ef update is itself idempotent)
dotnet ef database update --project src/Sygnia.Backend/src/Sygnia.Infrastructure --startup-project src/Sygnia.Backend/src/Sygnia.Presentation

Start-Process -FilePath "dotnet" -ArgumentList "run --project src/Sygnia.Backend/src/Sygnia.Presentation" -PassThru
Start-Process -FilePath "dotnet" -ArgumentList "run --project src/Sygnia.Wcf.Gateway" -PassThru

Write-Host "Sygnia.Presentation and Sygnia.Wcf.Gateway started. Run with -Stop to shut everything down."
```

(Replace the `dotnet ef database update` line with whatever Step 1 actually found — e.g. a `sqlcmd -i schema.sql` call if seeding is a raw script, guarded by a check for whether it already ran.)

- [ ] **Step 3: Test the happy path**

Run: `pwsh scripts/run-local.ps1`
Expected: containers come up, schema/seed applies without erroring on a second run, both .NET processes start.

- [ ] **Step 4: Test the stop path**

Run: `pwsh scripts/run-local.ps1 -Stop`
Expected: both processes stop, containers go down.

- [ ] **Step 5: Commit**

```bash
git add scripts/run-local.ps1
git commit -m "build: add run-local.ps1 to seed DB and start/stop the local stack"
```

---

## Self-review notes

- Task 1's exposed port and Task 3's env var names are marked as best-guesses pending a check against `Sygnia.Presentation/appsettings.json` — the executing agent must verify these against the real file rather than trusting the plan blindly.
- Task 4 explicitly requires locating the real seed mechanism before writing the script, rather than assuming EF migrations exist.
- `Sygnia.Wcf.Gateway`/`Sygnia.WpfClient` are deliberately excluded from docker-compose per the Windows-only constraint already documented in root CLAUDE.md.
