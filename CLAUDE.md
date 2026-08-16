# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository state

**Greenfield — no code exists yet.** `src/` is empty; there is no solution file or build tooling. Everything currently in the repo is planning material:

- `docs/Senior_C___NET_Developer__Backend__Assignment.pdf` — the assignment brief and **authoritative requirements source**. A PDF, so read it explicitly when requirements are in question rather than inferring from the notes.
- `docs/first_draft.md` — the worked plan and **design of record**.
- `docs/planning.md` — the developer's raw, out-of-order notes. High-level intent; deliberately names no projects. Source of the coding standards (§11) and the diagram list (§12).
- `planning/` — numbered implementation steps, one file per step (`one_…`, `two-…`). **A `-done` suffix in the filename means that step is complete**; the current step is the highest-numbered file without one. A step whose guidance stays useful after it ships may instead be folded into a nested `CLAUDE.md` in the project it describes — as the domain step was, into `src/Sygnia.Backend/Sygnia.Domain/CLAUDE.md`.

**Nested `CLAUDE.md` files** live beside the code they govern and load automatically when working in that folder. `src/Sygnia.Backend/Sygnia.Domain/CLAUDE.md` holds the domain-layer rules; read it before touching that project rather than relying on the summary here.

**Document precedence — the more specific document wins.** These three operate at descending levels of abstraction, and they have already drifted apart once:

`docs/planning.md` (high-level intent) → `docs/first_draft.md` (design of record) → the current `planning/` step file (**overrides both**)

When the step file contradicts `first_draft.md`, the step file is right and `first_draft.md` should be reconciled to it — not the other way round. Note that `first_draft.md` still cites `planning/one.md` by name in several places; those are historical references to the scaffold step, now `docs/project-setup.md`.

There are **no build/test/lint commands yet**. After scaffolding, replace this section with the real ones. The intended commands (from `first_draft.md`):

```bash
dotnet build && dotnet test
dotnet test --filter FullyQualifiedName~Concurrent   # the idempotency-under-race test
dotnet test --filter FullyQualifiedName~Statement    # the 50k-row streaming test
dotnet run --project src/Sygnia.Backend/Sygnia.Presentation
ng build                                              # from src/Sygnia.Frontend
docker compose up -d                                  # SQL Server + Seq + Jaeger + host + SPA
```

## Scope: what is in and what was deliberately cut

`planning.md` lists more than the brief asks for, and the scope has been narrowed to the following. Do not add anything from the Out list without being asked:

- **In:** gRPC service (submit / transfer / balance / streaming statement), SQL Server via EF Core, Testcontainers integration tests, an **Angular 18 front end** (`Sygnia.Frontend`), Serilog→Seq, OpenTelemetry→Jaeger, docker-compose, `README.md` + `SOLUTION.md`.
- **Out:** Redis, Swagger, GitHub Pages. Each is recorded in `SOLUTION.md` as a deliberate omission.
- **Opt-in:** MediatR and its pipeline behaviours — out at the root scope, but a project's own nested `CLAUDE.md` may opt back in for itself (as `Sygnia.Application`'s does). A nested opt-in overrides this section for that project only, per the document-precedence rule above; record the opt-in and its reasoning in `SOLUTION.md`.
- **Last, and droppable:** the .NET Framework 4.8 WCF gateway (NetTcp, one `GetBalance` operation acting as a gRPC client). Windows-only; sequenced last so it cannot block the core.

The front end was originally out of scope and the scaffold step (`docs/project-setup.md`) put it back in. Sequencing still matters: **the backend core must be green before frontend work starts**, because the core is what the assignment grades and the front end adds no marks of its own. Ask before dropping any scope item rather than deciding silently.

## The two invariants that carry the solution

Most of the code is plumbing. These two are what the assignment grades, and both fail *silently* if violated.

**1. Idempotency lives in the database, not in C#.** The composite primary key `(AccountId, ExternalRef)` **is** the idempotency mechanism. Any `SELECT`-then-`INSERT` in application code loses the race the brief explicitly tests. The flow is: attempt the INSERT → on SQL error 2627/2601, read the stored row back and compare amount/currency/occurredAt → identical means idempotent replay (return the stored row, OK); different means `ALREADY_EXISTS` naming the conflicting fields. The SQL exception is caught at the infrastructure boundary and never escapes it as an exception — it becomes a `Result`.

**2. Statements stream end to end, or they are wrong.** A single `.ToListAsync()` anywhere in the statement path defeats the requirement while every functional test still passes. The path is EF Core `AsAsyncEnumerable()` + `AsNoTracking()` → repository returns `IAsyncEnumerable<Movement>` → handler yields rows while accumulating a running total in one `decimal` → gRPC server streaming `WriteAsync` per row → client reads one row at a time. The 50k-row memory test is the only guard against regression here. The Angular side is bound by the same rule: render rows as they arrive, never collect the stream into an array first.

## Architecture

Clean Architecture, dependencies inward only. Each project exposes public interfaces plus exactly one DI extension; everything else is `private sealed`.

```
src/
├─ Sygnia.Frontend/                    Angular 18 SPA — gRPC-Web clients
└─ Sygnia.Backend/                     .NET 8 solution
    ├─ Sygnia.Backend.sln              src/ and tests/ also exist as solution folders
    ├─ global.json                     pins SDK 8.0.319
    ├─ Directory.Build.props           shared build properties
    ├─ Directory.Packages.props        central package management
    ├─ src/
    │   ├─ Sygnia.Domain/              no dependencies. Models/ (Movement, Account, User),
    │   │                              Helpers/ (Guard, Result<T>, Error). AddDomain()
    │   ├─ Sygnia.Application/         ports (IMovementRepository, IBalanceReader,
    │   │                              IStatementReader), private sealed handlers,
    │   │                              FluentValidation. AddApplication()
    │   ├─ Sygnia.Infrastructure/      DbContext + composite-key config, repositories incl.
    │   │                              the 2627 catch. AddInfrastructure(connectionString)
    │   ├─ Sygnia.Presentation/        gRPC host + composition root. Protos/, ErrorInterceptor,
    │   │                              Result<T> → StatusCode mapping. Program.cs is four AddX()
    │   └─ Sygnia.Wcf.Gateway/         .NET Framework 4.8 — built LAST
    └─ tests/
        └─ Sygnia.Tests/               unit tests now, integration tests later — hence the
                                       plain name rather than Sygnia.UnitTests
```

The `src` and `tests` directories are mirrored by **solution folders** of the same names in
`Sygnia.Backend.sln`, so Solution Explorer and disk agree.

**Tests live *inside* `src/Sygnia.Backend/`, not beside `src/`.** They have to: `global.json` and the two `Directory.*.props` files sit at `src/Sygnia.Backend/`, and MSBuild only discovers them by walking *up* from a project. A test project at the repo root would silently miss all three — resolving the .NET 10 preview SDK instead of 8, and losing central package management.

Reference direction — easiest thing to get wrong in a scaffold, hardest to unpick later:

```
presentation → application → domain
infrastructure → application → domain
presentation → infrastructure   (composition root only, to call AddInfrastructure)
domain → nothing
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
- **The front end forces gRPC-Web.** A browser cannot speak native gRPC (no access to HTTP/2 trailers), so `Sygnia.Presentation` must enable `Grpc.AspNetCore.Web` — `UseGrpcWeb()` plus `EnableGrpcWeb()` on the endpoint — and CORS for the Angular origin. gRPC-Web *does* support server streaming (it drops only client and bidirectional streaming), so `StreamStatement` survives the browser hop intact.

## Known traps

- Testcontainers needs a running Docker daemon — verify Docker works *before* building the integration harness, not after.
- `private sealed` types cannot be tested directly from another assembly. Test through the public interface (preferred), or use `internal sealed` with `[assembly: InternalsVisibleTo]` where a test genuinely needs the concrete type.
- The WCF gateway mixes a legacy `.csproj` into an SDK-style solution and is Windows-only.
- `Sygnia.Wcf.Gateway` compiles `movements.proto` independently of `Sygnia.Presentation` (its own `<Protobuf>` item, `GrpcServices="Client"`), but the proto hardcodes `option csharp_namespace = "Sygnia.Presentation";`. Both projects therefore export their own full copy of the `Sygnia.Presentation.*` message/client types. Nothing breaks while no project references both assemblies, but never let anything reference `Sygnia.Wcf.Gateway` and `Sygnia.Presentation` together (directly or transitively) — every shared type collides on CS0433 (ambiguous reference).

## Workflow

1. **Always create a local branch before starting** a feature or bug fix.
2. **NEVER commit to `main` or make code changes on `main`.**
3. When the feature is complete, open a PR.
4. **The PR must be approved before merging.** Approval is the user's — never self-merge. Open the PR, report it, and stop there; the user reviews, approves, and says when to merge.
5. Once merged, delete the feature or bug branch (local and remote).
6. **Read the current step file in `planning/`** — the highest-numbered one *without* a `-done` suffix — and execute it. It is an instruction to act on, not just context to read. When it is finished, rename it with a `-done` suffix.
7. For .NET projects, keep build config centralised in `Directory.Build.props` / `Directory.Packages.props` — never repeat `TargetFramework` or package versions in a `.csproj`.
8. Keep `node_modules` out of git.

**Renaming a file by case only needs a two-step `git mv`** (`Foo` → `tmp` → `FOO`). Windows is case-insensitive, so a direct rename leaves git tracking the old path while disk shows the new one — it builds locally and breaks on any case-sensitive checkout.