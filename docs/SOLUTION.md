# Solution

## Architecture

Clean Architecture, dependencies pointing inward only: `Presentation → Application → Domain`,
`Infrastructure → Application → Domain`, with `Presentation → Infrastructure` restricted to the
composition root (`Program.cs`, to call `AddInfrastructure`). This maps directly onto the
assignment's own task breakdown — domain/persistence (Task 1) → RPC surface (Task 2) → optional
legacy gateway (Task 3) — and keeps the two correctness-critical invariants below isolated in the
layer that owns them: `Sygnia.Domain` cannot reach EF Core, so idempotency logic cannot leak out
of the infrastructure boundary.

## Task 1 — Domain and persistence

**Movements** are persisted with a composite primary key `(AccountId, ExternalRef)` — that key
*is* the uniqueness/duplicate-handling mechanism the brief asks for, not a check in C#.
`MovementRepository.AddAsync` never does a `SELECT` before the `INSERT`; a `SELECT`-then-`INSERT`
would lose the race the brief explicitly tests (two callers submitting the same
`(accountId, externalRef)` at the same instant). Instead:

1. Attempt the `INSERT` unconditionally.
2. On SQL error 2627 (unique constraint) or 2601 (unique index), read the stored row back and
   compare `Amount`, `Currency` and `OccurredAt` against what was just submitted.
3. **Identical** → idempotent replay: return the stored row as success.
4. **Different** → a conflict (see [Field-conflict choice](#field-conflict-choice) below).

`Transfer` writes both legs of a movement in one transaction, resolving each leg's conflict
independently before deciding anything, since a conflict on either leg rolls the whole
`SaveChangesAsync` back.

**Balance** is a `SUM(Amount)` aggregate computed on read, served from an index on
`(AccountId, OccurredAt) INCLUDE (Amount)` so the aggregate is satisfied from the index alone.

**Tests:** duplicate handling under concurrency (multiple callers submitting the same reference
at the same time) and balance math are covered with Testcontainers against a real SQL Server
instance, not a mocked/in-memory provider — the concurrency behaviour depends on the database's
actual unique-constraint enforcement.

**Logging:** structured logs (Serilog → Seq) at movement submission, conflict, and transfer.

## Task 2 — Modern RPC (gRPC on .NET 8)

Protobuf messages/service cover submitting a movement, retrieving current balance, and exporting
a statement for a date range. Money crosses the wire as a string decimal, not `double` —
protobuf has no decimal type and `double` would corrupt cents.

**Large statement exports** stream end to end rather than buffering: EF Core `AsAsyncEnumerable()`
+ `AsNoTracking()` → repository returns `IAsyncEnumerable<Movement>` → handler yields rows while
accumulating a running total in one `decimal` → gRPC server streaming `WriteAsync` per row. No
`.ToListAsync()` exists anywhere on this path — that call would satisfy every functional test
while still holding the full 50,000+ row result in memory, which is exactly what the brief asks
to avoid.

### Running total is only carried by the streaming path

The brief asks for a statement to be "an ordered list of movements in a date range with a
running total." There are two statement RPCs, and only one of them computes it:

- **`GetStatement`** (server streaming) — the running total is computed per row as the stream is
  produced, and carried on the wire in `StatementLine.RunningTotal`. This backs the "Stream full
  statement" button and the PDF export.
- **`GetStatementPage`** (unary, paged) — backs the default "Search" table, the first view most
  users hit. It deliberately leaves `RunningTotal` unset on every row, and the frontend table has
  no running-total column at all.

This is intentional, not an oversight: a running total computed over a single page in isolation
isn't the account's real running total — it would only be the sum of that page's rows, and
showing it next to a number that looks like a genuine cumulative balance would be actively
misleading. Rather than compute and show a number that lies, the paged path omits it entirely;
the only place a user sees a trustworthy running total is the full, ordered stream, where it's
unambiguous. The trade-off: a user working entirely from the paginated table never sees a running
total on screen unless they also stream the full statement or download the PDF.

### Error approach

One error handler per transport: a server-side `Interceptor` (`ErrorInterceptor`) wraps every
unary and server-streaming call, passes through anything already a deliberate `RpcException`, and
converts any other exception into a status-only `INTERNAL` — the real exception is logged, never
put on the wire. Status mapping happens in exactly one place, so no service method decides its own
status codes:

| Condition | Status |
|---|---|
| validation failure | `INVALID_ARGUMENT` |
| unknown account | `NOT_FOUND` |
| duplicate reference, conflicting fields | `ALREADY_EXISTS` (conflicting fields named in the message) |
| anything unexpected | `INTERNAL` |

## Task 3 — Legacy gateway (optional) — implemented

`Sygnia.Wcf.Gateway` is a self-hosted WCF NetTcp service exposing one operation,
`GetBalance(accountId)`, which calls the same gRPC `GetBalance` RPC the modern clients use — so
the legacy and modern entry points return identical balances for identical data, by construction
rather than by duplicated logic. Run instructions and configuration are in `README.md`.

Error handling matches the same one-handler-per-transport approach as Task 2: an `IErrorHandler`
attached via a service behavior catches anything unhandled and turns it into a clean
`FaultException`; no exception details reach the wire.

## Field-conflict choice

The brief asks: if a repeated `externalRef` for an account differs from the original submission
(different amount, currency, etc.), how should that be surfaced, without double-counting?

Chosen approach: `ALREADY_EXISTS`, with the error naming exactly which fields conflict (amount,
currency, occurred-at), rather than a generic "duplicate" message.

- The caller needs to distinguish *"this exact submission already succeeded"* (safe to treat as
  success — return the stored movement) from *"something with this reference already exists but
  doesn't match"* (a real conflict needing attention), and a single generic error code can't carry
  that distinction.
- Naming the conflicting fields turns an investigation from "which field changed?" into a
  one-line answer.
- `ALREADY_EXISTS` is the closest fit in the gRPC status vocabulary; `INVALID_ARGUMENT` would be
  wrong because the request itself is well-formed — it just collides with prior state.

New and legacy entry points share the same repository and conflict logic, so both produce
consistent outcomes over the same data, as the brief requires.

## Simplifications made

- **Balance is computed on read** rather than maintained as a running, materialised value.
  Simpler and always consistent with the movement ledger by construction; the trade-off is an
  O(n) aggregate per balance read instead of O(1), a non-issue at the volumes in this assignment.
- **No maker/checker approval flow.** A movement is recorded and immediately live. Not required
  by the brief.
- **Credentials in local config for dev simplicity**, rather than a secrets manager — acceptable
  for a local take-home run, not for a real deployment.
- **Optional extensions not built:** batch submission, file-format statement export, a
  progress-showing client utility. All explicitly "only if time allows" in the brief and cut for
  time.
