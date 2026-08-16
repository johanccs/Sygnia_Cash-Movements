# Solution

## Architecture

Clean Architecture, dependencies pointing inward only: `Presentation → Application → Domain`,
`Infrastructure → Application → Domain`, with `Presentation → Infrastructure` restricted to the
composition root (`Program.cs`, to call `AddInfrastructure`). Each project exposes public
interfaces plus exactly one `AddX()` DI extension; everything else is `private sealed`, so an
implementation cannot be registered from outside the layer that owns it — that constraint is
what makes the one-`Add<Layer>()`-per-project rule actually hold.

**Alternatives considered:**
- Modular monoliths
- Vertical slices
- For more complex applications, use event sourcing
- If the application is to be broken into microservices, use event-driven architecture

None of these were chosen for the current scope. Clean Architecture's explicit dependency
direction made the two invariants below easier to defend in review: a reviewer can see that
`Sygnia.Domain` cannot reach EF Core, so idempotency logic cannot leak out of the infrastructure
boundary — and the layered structure maps directly onto the assignment's own task breakdown
(domain/persistence → RPC surface → optional gateway).

**ORM:** EF Core 8. See `Sygnia.Infrastructure/CLAUDE.md` for the layer-specific rules
(`AsNoTracking()` for every read path, `IAsyncEnumerable` for the statement query).

**CQRS via MediatR** — opted back in for `Sygnia.Application` only (out at the solution root;
see [Deliberate scope omissions](#deliberate-scope-omissions)) — for loose coupling between the
gRPC service layer and the handlers. The alternative was injecting repositories directly into
`MovementGrpcService`; MediatR was chosen so the transport layer stays a thin mapper (wire
message in, `Result<T>` out) with no business logic of its own.

## The two invariants

### 1. Idempotency lives in the database, not in C#

The composite primary key `(AccountId, ExternalRef)` on `Movements` **is** the idempotency
mechanism. `MovementRepository.AddAsync` never does a `SELECT` before the `INSERT` — that
would lose the race the brief explicitly tests (two callers submitting the same
`(accountId, externalRef)` at the same instant). Instead:

1. Attempt the `INSERT` unconditionally.
2. On SQL error 2627 (unique constraint) or 2601 (unique index), read the stored row back and
   compare `Amount`, `Currency` and `OccurredAt` against what was just submitted.
3. **Identical** → idempotent replay: return the stored row as success (`OK`).
4. **Different** → `movement.already_exists`, naming the specific fields that differ.

The SQL exception never escapes the infrastructure boundary as an exception — it's caught in
`MovementRepository` and turned into a `Result<Movement>` before it reaches `Sygnia.Application`.
`Transfer` follows the same shape but resolves both legs independently before deciding anything,
since a conflict on either leg rolls the whole `SaveChangesAsync` back and the other leg reading
back "not found" is then expected, not itself a fault (see the doc comments on
`MovementRepository.ResolveTransferConflictAsync`).

Covered by `MovementRepositoryConcurrentTests` and `MovementRepositoryTransferConflictTests`
(Testcontainers, real SQL Server, concurrent submissions from multiple callers).

### 2. Statements stream end to end

`StatementReader.StreamAsync` uses `AsAsyncEnumerable()` + `AsNoTracking()`; the repository
returns `IAsyncEnumerable<Movement>`; the query handler yields rows while accumulating a
running total in a single `decimal`; `MovementGrpcService.GetStatement` writes each row to the
gRPC response stream as it's produced. No `.ToListAsync()` exists anywhere on this path — that
call would defeat the requirement while every *functional* test still passed, which is why the
50k-row test (`StatementReaderTests`) is the load-bearing guard here rather than a correctness
assertion alone.

## Field-conflict choice

The brief asks: if a repeated `externalRef` differs from the original (different amount,
currency, etc.), how should that be surfaced? Chosen approach: `ALREADY_EXISTS`, with the
error message naming exactly which fields conflict (`Amount`, `Currency`, `OccurredAt`), rather
than a generic "duplicate" message. Reasoning:
- The caller needs to distinguish *"this exact submission already succeeded"* (safe to treat as
  success — `OK` with the stored movement) from *"something with this reference already exists
  but doesn't match"* (a real conflict requiring human attention), and a single generic error
  code can't carry that distinction.
- Naming the conflicting fields in the message (not just the code) turns an operations
  investigation from "which of five fields changed?" into a one-line answer, without needing a
  diff endpoint.
- `ALREADY_EXISTS` is the closest fit in the standard gRPC status vocabulary; `INVALID_ARGUMENT`
  would be wrong because the request itself is well-formed — it just collides with prior state.

## Error approach (gRPC)

The brief asks for one global error handler per transport, analogous to HTTP's
`ProblemDetails`. Since this service speaks gRPC rather than HTTP, the direct equivalent is a
server-side `Interceptor`: `ErrorInterceptor` wraps every unary and server-streaming call,
passes through anything already a deliberate `RpcException`, and converts any other exception
into a status-only `INTERNAL` — the real exception is logged via Serilog, never put on the
wire. This is the one place `SOLUTION.md` needs to record as a substitution for `ProblemDetails`
per the root `CLAUDE.md`.

Status mapping happens in exactly one place, `ResultExtensions.ToRpcException`, so no service
method decides its own status codes:

| `Error.Code` | Status |
|---|---|
| validation failure (`*.invalid`) | `INVALID_ARGUMENT` |
| `account.not_found` | `NOT_FOUND` |
| `movement.already_exists` | `ALREADY_EXISTS` (conflicting fields in the message) |
| anything unexpected | `INTERNAL` |

Constructors throw for broken invariants (a `Movement` that could never legitimately exist);
`Result<T>` carries expected business outcomes (duplicate ref, unknown account) back to the
caller. These aren't in tension — see `Sygnia.Domain/CLAUDE.md` for the full reasoning.

## Simplifications made

- **Balance is computed on read** (`SUM(Amount)` over `Movements`, served from the
  `(AccountId, OccurredAt) INCLUDE (Amount)` index) rather than maintained as a running,
  materialised value. Simpler and always consistent with the movement ledger by construction;
  the trade-off is an O(n) aggregate per balance read instead of O(1). At the volumes in this
  assignment (an account with tens of thousands of movements) this is a non-issue; a
  materialised balance with incremental updates is the documented next step if read volume ever
  makes the aggregate itself the bottleneck.
- **No maker/checker approval flow.** A movement is recorded and immediately live. In a real
  version of this system the person who submits a movement would not be the person who can
  approve it — noted as a next step, not built, since the brief doesn't ask for it.
- **Credentials in `appsettings.json`.** The SQL Server password is plaintext in
  `Sygnia.Presentation/appsettings.json` for local-dev simplicity. A real deployment would use
  user secrets locally and a secret manager (Azure Key Vault / AWS Secrets Manager) in the
  pipeline, with periodic rotation.
- **`Currency` is validated as three letters but not case-normalised** — `"zar"` and `"ZAR"`
  would be accepted as distinct values today. Flagged in `Sygnia.Domain/CLAUDE.md` as a known
  gap rather than silently fixed, since no test currently covers the behaviour either way.
- **No maker/checker, no batching, no file export** — all explicitly optional in the brief and
  cut for time.

## Legacy gateway (Task 3, optional) — implemented

`Sygnia.Wcf.Gateway` is a self-hosted WCF NetTcp service exposing one operation,
`GetBalance(accountId)`, that calls the same gRPC `GetBalance` RPC the Angular frontend and
`grpcurl` use — so the legacy and modern entry points are guaranteed to return identical
balances for identical data, by construction rather than by duplicated logic.

**HTTP/2 from .NET Framework:** `Grpc.Net.Client`'s default `HttpClientHandler` on .NET
Framework doesn't support HTTP/2 trailers, so the gateway uses `System.Net.Http.WinHttpHandler`
instead — the same class of constraint that forces gRPC-Web on the browser side, solved
differently here since the gateway is a native .NET Framework process, not a browser.

**Error handling:** matches the "one global error handler per transport" rule elsewhere in the
solution — a `GrpcErrorHandler` (`IErrorHandler`) attached via a service behavior catches
anything unhandled and turns it into a clean `FaultException<BalanceFault>`; `RpcException`s
from the gRPC call are mapped the same way in `BalanceService` itself. No exception details
reach the wire.

**Client:** `Sygnia.WpfClient`, a minimal WPF app, plays the "legacy tool" consuming the
gateway — it shares the WCF contract types directly (linked files, both projects being net48)
rather than generating a service reference.

**Simplification:** no retry/resilience policy on either hop (WCF→gRPC or WPF→WCF) — a single
attempt, with the failure surfaced to the user. No authentication on the NetTcp endpoint,
matching the rest of the take-home's unauthenticated surface. Note that `SecurityMode.None`
disables NetTcp *transport encryption* as well as authentication — not just "no
authentication" — so traffic between `Sygnia.WpfClient` and `Sygnia.Wcf.Gateway` is plaintext
on the wire. A real deployment would use `SecurityMode.Transport` with Windows authentication
instead of `SecurityMode.None`.

## Deliberate scope omissions

- **Redis, Swagger** — cut from `planning.md`'s broader scope; not needed to satisfy the
  assignment brief. Swagger in particular doesn't apply here regardless: it's a REST/OpenAPI
  tool with no visibility into a binary gRPC service without a separate REST/gRPC gateway,
  which is out of scope.
- **GitHub Pages** — originally scoped out per root `CLAUDE.md` (listed alongside Redis and
  Swagger as "Out"). Implemented anyway on 2026-08-16 per explicit user request, as a static
  landing page served from `/docs` on `main` (`docs/index.html`) advertising the project and
  linking to the design manual (`docs/Sygnia-Design-Manual.docx`) and the repository. This is a
  deliberate, requested exception to the root scope decision, not a reversal of the reasoning
  behind it — the manual grading criteria still don't require it. Enabling Pages itself (repo
  Settings -> Pages -> source: `main` / `/docs`) is a repo-settings change outside git and was
  left for the user to flip on.
- **MediatR / pipeline behaviours** — out at the solution root, but opted back in for
  `Sygnia.Application` specifically (see
  `src/Sygnia.Backend/src/Sygnia.Application/CLAUDE.md`): commands/queries as records, private
  sealed handlers, FluentValidation, and logging + validation pipeline behaviours, registered via
  an `AppModuleExtensions` DI extension. Scoped to that project rather than solution-wide to keep
  the dependency out of `Sygnia.Domain` and `Sygnia.Presentation`. `ValidationBehaviour<,>` runs
  after `LoggingBehaviour<,>` and replaces the validate-then-map-to-`Error` boilerplate that used
  to be repeated in each of the four command handlers; a request opts in by implementing the
  internal `IValidatedRequest` marker (naming the `ErrorCode` to report on failure), so the
  behaviour safely no-ops for any request — including the streaming `GetStatement` query — that
  doesn't implement it.
- **`ErrorCode` enum** (`Sygnia.Domain`) — replaces the raw string literals (`"movement.invalid"`,
  `"account.contactperson.invalid"`, etc.) previously passed to `new Error(code, message)`.
  `Error.Code` stays a `string` (least invasive: `ResultExtensionsTests` and other tests already
  assert against string codes, and gRPC status mapping is naturally string-keyed), but every call
  site now constructs an `Error` from an `ErrorCode` via `new Error(ErrorCode.X, message)`, which
  delegates to `ErrorCodeExtensions.ToCode()` — the single place the enum-to-wire-string
  convention lives. `ResultExtensions.ToRpcException` matches on the known codes derived from the
  enum first, falling back to the old `.EndsWith(".invalid")` heuristic only for a string that
  isn't one of the enum's known codes. `ResultExtensionsTests.ToRpcException_MapsEveryKnownErrorCode`
  enumerates every `ErrorCode` value and pins it to its expected `StatusCode`, so a new code added
  to the enum without a corresponding mapping fails the test instead of silently defaulting to
  `INTERNAL`.
- **Currency case normalisation** — `Guard.NormalizeCurrency` upper-invariants the currency code
  at construction time in both `Account` and `Movement`, and `Account.EnsureCurrencyMatches`
  (the single shared currency-mismatch check used by both `SubmitMovementCommandHandler` and
  `TransferFundsCommandHandler`) normalises the incoming value before comparing. `"zar"` and
  `"ZAR"` are now the same currency everywhere; see
  `src/Sygnia.Backend/src/Sygnia.Domain/CLAUDE.md`'s former "Known gap" note, now marked resolved.
- **Optional extensions** (batch submission, file-format statement export, a progress-showing
  client utility) — not built; all explicitly "only if time allows" in the brief.

## Things to consider

3. As the application evolves, the next step is to ensure there is a maker/checker procedure
   in the movement use case. This is to ensure the person who initiated the move cannot approve
   it. We need a second person to approve.
4. Currently the DB details are in `appsettings.json` for simplicity, but in a real-world
   project developing locally would make use of application secrets and not `appsettings.json`.
   The credentials would be overwritten in deployment pipelines with values from Azure DevOps or
   AWS Secret Manager. These could also be rotated on a weekly basis to improve security.
5. Use CQRS for loose coupling. Could have injected services directly into the controllers to
   execute business rules instead — that's the alternative that was considered and rejected,
   specifically because it would blur the "thin transport, no business logic" line the
   `Presentation` layer is meant to hold.
