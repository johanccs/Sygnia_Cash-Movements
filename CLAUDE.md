# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository state

**Greenfield — no code exists yet.** `src/` is empty; there is no solution file or build tooling. Everything currently in the repo is planning material:

- `docs/Senior_C___NET_Developer__Backend__Assignment.pdf` — the assignment brief and **authoritative requirements source**. A PDF, so read it explicitly when requirements are in question rather than inferring from the notes.
- `docs/first_draft.md` — the worked plan. **This is the current design of record** and supersedes `planning.md` wherever they disagree.
- `docs/planning.md` — the developer's raw, out-of-order notes. Source of the coding standards (§11) and the diagram list (§12).
- `planning/one.md` — the step pointer for the current implementation step.

There are **no build/test/lint commands yet**. After scaffolding, replace this section with the real ones. The intended commands (from `first_draft.md`):

```bash
dotnet build && dotnet test
dotnet test --filter FullyQualifiedName~Concurrent   # the idempotency-under-race test
dotnet test --filter FullyQualifiedName~Statement    # the 50k-row streaming test
dotnet run --project src/CashMovements.Grpc
docker compose up -d                                  # SQL Server + Seq + Jaeger + host
```

## Scope: what is in and what was deliberately cut

`planning.md` lists more than the brief asks for. `first_draft.md` cut it back — do not reintroduce these without being asked:

- **In:** gRPC service (submit / transfer / balance / streaming statement), SQL Server via EF Core, Testcontainers integration tests, Serilog→Seq, OpenTelemetry→Jaeger, docker-compose, `README.md` + `SOLUTION.md`.
- **Out:** Redis, Swagger, MediatR and its pipeline behaviours, the UI component layer, GitHub Pages. Each is recorded in `SOLUTION.md` as a deliberate omission.
- **Last, and droppable:** the .NET Framework 4.8 WCF gateway (NetTcp, one `GetBalance` operation acting as a gRPC client). Windows-only; sequenced last so it cannot block the core.

## The two invariants that carry the solution

Most of the code is plumbing. These two are what the assignment grades, and both fail *silently* if violated.

**1. Idempotency lives in the database, not in C#.** The composite primary key `(AccountId, ExternalRef)` **is** the idempotency mechanism. Any `SELECT`-then-`INSERT` in application code loses the race the brief explicitly tests. The flow is: attempt the INSERT → on SQL error 2627/2601, read the stored row back and compare amount/currency/occurredAt → identical means idempotent replay (return the stored row, OK); different means `ALREADY_EXISTS` naming the conflicting fields. The SQL exception is caught at the persistence boundary and never escapes it as an exception — it becomes a `Result`.

**2. Statements stream end to end, or they are wrong.** A single `.ToListAsync()` anywhere in the statement path defeats the requirement while every functional test still passes. The path is EF Core `AsAsyncEnumerable()` + `AsNoTracking()` → repository returns `IAsyncEnumerable<Movement>` → handler yields rows while accumulating a running total in one `decimal` → gRPC server streaming `WriteAsync` per row → client reads one row at a time. The 50k-row memory test is the only guard against regression here.

## Architecture

Clean Architecture, dependencies inward only. Each project exposes public interfaces plus exactly one DI extension; everything else is `private sealed`.

```
src/
├─ CashMovements.Domain/       no dependencies. Movement, Account, Result<T>, Error. AddDomain()
├─ CashMovements.Application/  ports (IMovementRepository, IBalanceReader, IStatementReader),
│                              private sealed handlers, FluentValidation. AddApplication()
├─ CashMovements.Persistence/  DbContext + composite-key config, repositories incl. the 2627
│                              catch. AddPersistence(connectionString)
├─ CashMovements.Grpc/         .NET 8 host + composition root. Protos/, ErrorInterceptor,
│                              Result<T> → StatusCode mapping. Program.cs is four AddX() calls
└─ CashMovements.Wcf.Gateway/  .NET Framework 4.8 — built LAST
tests/
├─ CashMovements.UnitTests/         domain guards, balance math, Result mapping
└─ CashMovements.IntegrationTests/  Testcontainers — real SQL Server, not in-memory
```

Why `private sealed` matters structurally: a private implementation cannot be named from another assembly, so the composition root *cannot* register it. Registration must happen inside the owning layer — which is exactly what the one-`Add<Layer>()`-per-project rule provides.

## Coding standards (binding, from planning.md §11)

- Every service gets an interface; implementations are `private sealed`.
- Primary constructors for injection.
- Domain properties readonly, set once in the constructor; guard clauses run **before** any field is assigned.
- **Constructors throw, methods return `Result<T>`.** These are not in conflict — they split failures in two. A broken invariant (null account id, blank currency) is an object that must never exist, so the constructor throws `ArgumentException`/`ArgumentOutOfRangeException` and the type system then guarantees every `Movement` is valid. An expected outcome (duplicate ref, unknown account) is a value the caller must handle, so it flows back as `Result<T>`.
- One global error handler per transport. `ProblemDetails` is HTTP-shaped and this service speaks gRPC, so the equivalent is a server `Interceptor` catching anything unhandled and rethrowing as `RpcException` with the right status (WCF side: an `IErrorHandler` producing a clean `FaultException`). Details go to the log, never to the wire. `SOLUTION.md` must record this substitution.
- **TDD per layer** — write the failing test, watch it fail for the right reason, then implement.

## Data and contract specifics

- `Movements` PK is `(AccountId VARCHAR(10), ExternalRef VARCHAR(20))` — e.g. `ACC-001`, `MOV-20240715-000123`. Design repositories and EF configuration around this, never a surrogate id.
- Amount is `DECIMAL(19,4)`, never `float`. **Sign carries meaning**: positive = deposit, negative = withdrawal. Movements also carry currency, `OccurredAt`, narration, a guid `RefNr`, `MovedBy`, `MovedDate`.
- `INDEX IX_Movements_Account_OccurredAt (AccountId, OccurredAt) INCLUDE (Amount)` serves both the balance `SUM` and the statement scan from the index alone.
- Balance is a `SUM(amount)` aggregate computed on read; the materialised-balance alternative is documented in `SOLUTION.md` rather than built.
- Transfers are one atomic `Transfer` RPC writing both legs in a single transaction.
- Money crosses the gRPC wire as a **string** decimal (`"12500.00"`), not a `double` — protobuf has no decimal type and `double` corrupts cents. Timestamps are `google.protobuf.Timestamp`, UTC.
- Status mapping, done once in the interceptor and service and nowhere else: validation → `INVALID_ARGUMENT`; unknown account → `NOT_FOUND`; same key + same fields → `OK` with the stored movement; same key + different fields → `ALREADY_EXISTS` plus the conflicting field names; anything unexpected → `INTERNAL`.

## Known traps

- Testcontainers needs a running Docker daemon — verify Docker works *before* building the integration harness, not after.
- `private sealed` types cannot be tested directly from another assembly. Test through the public interface (preferred), or use `internal sealed` with `[assembly: InternalsVisibleTo]` where a test genuinely needs the concrete type.
- The WCF gateway mixes a legacy `.csproj` into an SDK-style solution and is Windows-only.

## Workflow

1. ALWAYS create a new local git branch before starting a feature or bug fix. NEVER commit directly to `main` or make code changes directly on `main`.
2. When a feature is complete, open a PR before merging to `main`.
3. Read `planning/one.md` for the current step.
