# Sygnia Cash Movements

A cash movements service for a financial administration platform: operations staff record
deposits, withdrawals and transfers, view account balances, and export large statements. Built
as a focused backend slice — SQL Server persistence, a gRPC API, and an Angular front end —
with correctness under duplicate and concurrent submissions as the core requirement.

See [`docs/SOLUTION.md`](docs/SOLUTION.md) for the design, trade-offs and deliberate scope
omissions, and [`CLAUDE.md`](CLAUDE.md) for the architecture and coding standards this repo
follows.

## Prerequisites

- [.NET SDK 8.0.319](https://dotnet.microsoft.com/download/dotnet/8.0) — pinned by
  [`global.json`](src/Sygnia.Backend/global.json)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) — running, for SQL Server /
  Seq / Jaeger and for the Testcontainers-based integration tests
- [Node.js 18+](https://nodejs.org/) and the Angular CLI (`npm install -g @angular/cli`) — for
  the frontend

## Run it locally

### 1. Start infrastructure

From the repo root:

```bash
docker compose up -d
```

This brings up SQL Server (`localhost:1433`, sa / `@1Mops4moa`), Seq
(`http://localhost:5341`) and Jaeger (`http://localhost:16686`).

### 2. Create the schema

Either apply the EF Core migration:

```bash
cd src/Sygnia.Backend
dotnet ef database update --project src/Sygnia.Infrastructure --startup-project src/Sygnia.Presentation
```

or run the equivalent SQL script directly (e.g. in SSMS or `sqlcmd`) against a fresh
`sygnia_cash` database — useful when you don't have the EF tooling installed:

```bash
sqlcmd -S localhost -U sa -P "@1Mops4moa" -d sygnia_cash -i src/Sygnia.Backend/scripts/00_create_schema.sql
```

### 3. Seed data

Run in order — each script is safe to re-run:

```bash
sqlcmd -S localhost -U sa -P "@1Mops4moa" -d sygnia_cash -i src/Sygnia.Backend/scripts/01_seed_accounts.sql
sqlcmd -S localhost -U sa -P "@1Mops4moa" -d sygnia_cash -i src/Sygnia.Backend/scripts/02_seed_users.sql
sqlcmd -S localhost -U sa -P "@1Mops4moa" -d sygnia_cash -i src/Sygnia.Backend/scripts/03_seed_statement_50000.sql
```

`01`/`02` seed two accounts (`ACC-001`, `ACC-002`) and three users. `03` seeds 50,000+ movements
on one account — the volume the streaming statement path is built for.

### 4. Run the gRPC host

```bash
cd src/Sygnia.Backend
dotnet run --project src/Sygnia.Presentation
```

Listens on `http://localhost:5058` (HTTP/2, gRPC + gRPC-Web) and `https://localhost:7110`.
Traces flow to Jaeger, structured logs to Seq and the console.

### Run the WCF gateway (optional, Task 3)

`Sygnia.Wcf.Gateway` is a minimal legacy NetTcp gateway exposing one `GetBalance` operation. It
calls into the same backend as the gRPC API — by acting as a gRPC client itself — so both entry
points return identical balances for identical data.

**Prerequisite:** run `dotnet dev-certs https --trust` on this machine first, if you haven't
already. The gateway's gRPC client uses `WinHttpHandler`, which validates the gRPC host's
ASP.NET Core dev certificate like any other TLS client — without a trusted dev cert the
handshake fails, which looks like a code bug rather than a missing local cert.

With the gRPC host already running (see above), in a second terminal:

```bash
dotnet run --project src/Sygnia.Backend/src/Sygnia.Wcf.Gateway
```

It listens on `net.tcp://localhost:8090/BalanceService`. Both addresses (the gRPC host it calls,
and the NetTcp address it listens on) are configurable in
`src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/App.config`.

### Run the WPF client (optional)

`Sygnia.WpfClient` is a minimal desktop app that queries the WCF gateway's `GetBalance`
operation — the "legacy tool" side of the demo. Windows only.

With the gRPC host and the WCF gateway both running (see above), in a third terminal:

```bash
dotnet run --project src/Sygnia.WpfClient
```

Enter an account ID (e.g. `ACC-001`) and click **Get Balance**. The gateway address is
configurable in `src/Sygnia.WpfClient/App.config`.

### 5. Run the frontend (optional)

```bash
cd src/Sygnia.Frontend
npm install
ng build   # or: ng serve
```

The SPA talks to the gRPC host over gRPC-Web; CORS for the Angular origin is enabled in
`Sygnia.Presentation`.

### Calling the service without the frontend

`movements.proto` defines `SubmitMovement`, `Transfer`, `GetBalance` and `GetStatement`
(server-streaming). Any gRPC client that can load a `.proto` file works — e.g.
[grpcurl](https://github.com/fullstorydev/grpcurl):

```bash
grpcurl -plaintext -import-path src/Sygnia.Backend/src/Sygnia.Presentation/Protos \
  -proto movements.proto -d '{"accountId":"ACC-001"}' \
  localhost:5058 movements.MovementService/GetBalance
```

## Build and test

```bash
cd src/Sygnia.Backend
dotnet build
dotnet test
```

Targeted runs for the two invariants the assignment grades:

```bash
dotnet test --filter FullyQualifiedName~Concurrent   # idempotency under concurrent submission
dotnet test --filter FullyQualifiedName~Statement    # 50k-row streaming (constant memory)
```

Integration tests use [Testcontainers](https://testcontainers.com/) and spin up their own SQL
Server container — Docker must be running, but the `docker compose` services above are not
required for `dotnet test` itself.

## Repository layout

```
src/
├─ Sygnia.Frontend/       Angular 18 SPA — gRPC-Web client
└─ Sygnia.Backend/        .NET 8 solution (Domain → Application → Infrastructure/Presentation)
    ├─ scripts/           schema creation (00) + seed data (01-03), run against SQL Server
    ├─ src/                Sygnia.Domain, Sygnia.Application, Sygnia.Infrastructure, Sygnia.Presentation
    └─ tests/Sygnia.Tests/ unit + integration tests
```

See [`CLAUDE.md`](CLAUDE.md) for the full architecture, the two invariants the solution is
built around (idempotency in the database, end-to-end statement streaming), and coding
standards.
