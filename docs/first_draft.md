# Cash Movements Service — First Draft Plan

## Context

`x:\Source\Sygnia\resources` is an empty repo holding only an assignment brief
(`docs/Senior_C___NET_Developer__Backend__Assignment.pdf`) and design notes
(`docs/planning.md`). Nothing has been built.

The brief asks for a working slice of a cash-movements backend: persist movements to
SQL Server, expose a gRPC API for submitting a movement, reading a balance, and
streaming a large statement, and never double-count when the same reference arrives
twice — including when two callers send it at the same instant. Deliverables are a
GitHub repo with `README.md` and `SOLUTION.md`, plus a 5–10 minute video walkthrough.
Estimated at ±4 hours.

The outcome we want: the three graded areas (service design, SQL correctness under
duplicates and concurrency, streaming gRPC) are airtight and provably tested, with
observability and a legacy WCF gateway added on top as bonus signal — without letting
those extras erode the core.

### Decisions already made

| Question | Decision |
|---|---|
| Scope | Brief-only core **plus** structured logging (Seq) and tracing (Jaeger) |
| WCF gateway | Designed now, **built last**, only if the core is green |
| Conflicting resubmit | Reject with gRPC `ALREADY_EXISTS` naming the mismatched fields |
| Balance | `SUM(amount)` aggregate on read; document the materialised alternative |
| Transfers | One atomic `Transfer` RPC writing both legs in a single transaction |
| Method | **TDD throughout** — `planning.md` line 65 makes this non-negotiable |

### Explicitly out of scope

Redis, Swagger, MediatR pipelines, the UI component layer, GitHub Pages. These appear
in `docs/planning.md` but not in the brief; each is noted in `SOLUTION.md` as a
deliberate omission rather than an oversight.

---

## Coding standards

From `planning.md` §11. These are binding on every layer, and several of them shape
the architecture rather than merely decorating it.

| Rule | How it shows up in the code |
|---|---|
| Every service gets an interface | `IMovementRepository`, `IStatementReader`, `IBalanceReader` |
| Implementations are `private sealed` | `private sealed class MovementRepository : IMovementRepository` |
| Primary constructors for injection | `internal sealed class Handler(IMovementRepository repo)` |
| Domain properties are readonly | `public decimal Amount { get; }` — set once, in the constructor |
| Constructors validate, then assign | Guard clauses run **before** any field is written |
| Invalid construction throws | `ArgumentException` / `ArgumentOutOfRangeException` |
| Methods return `Result<T>`, never throw | Expected business failures are values |
| One global error handler | Central, leak-proof mapping to transport errors |
| One DI extension per layer | `AddDomain()`, `AddApplication()`, `AddPersistence()`, `AddGrpcServices()` |
| TDD per layer | Test first, watch it fail, then implement |

### Two clarifications worth writing down

**"Throw in constructors" and "return `Result` from methods" are not in conflict.**
They divide failures into two kinds:

- A `Movement` with a negative-length currency code, or a null account id, is a
  **broken invariant** — an object that must never exist. The constructor throws, and
  the type system then guarantees every `Movement` in the system is valid.
- A duplicate `externalRef`, or an unknown account, is an **expected outcome** the
  caller is supposed to handle. The method returns `Result<T>`, and the compiler
  forces the caller to deal with it.

The payoff: once a `Movement` exists, no code anywhere needs to re-check it.

**`ProblemDetails` is HTTP-shaped; this service speaks gRPC.** Line 62's intent — one
central place that converts failures into responses and never leaks internals — is
right, but the mechanism differs by transport:

- **gRPC** → a server `Interceptor` catching anything unhandled and rethrowing as
  `RpcException` with the right `StatusCode`. Details go to the log, never to the wire.
- **WCF gateway** → an `IErrorHandler` translating into a clean `FaultException`.

`SOLUTION.md` records this substitution explicitly, so it reads as a considered
adaptation rather than a missed requirement.

**Why `private sealed` forces the DI extensions.** A `private` implementation cannot
be named from another assembly, so the composition root *cannot* write
`services.AddScoped<IMovementRepository, MovementRepository>()`. The registration must
happen inside the owning layer, which is precisely what one static
`Add<Layer>()` extension per project provides. `Program.cs` then reads as four lines.

---

## The two ideas that carry the whole solution

Everything else is plumbing. These two are what the assignment actually grades.

### 1. Idempotency belongs to the database, not to C#

The composite primary key `(AccountId, ExternalRef)` **is** the idempotency mechanism.
Any `SELECT`-then-`INSERT` in application code loses the race the brief explicitly
tests. So we insert first and let the database referee:

```
try INSERT the movement
├─ success              → new movement recorded
└─ SQL error 2627/2601  → the key already exists (unique key violation)
   └─ read the stored row back and compare amount, currency, occurredAt
      ├─ fields identical  → idempotent replay; return the stored movement, OK
      └─ fields differ     → ALREADY_EXISTS, listing which fields conflict
```

Two concurrent identical submits: one wins the insert, the other takes the violation
branch, reads back an identical row, and returns success. One movement stored, both
callers happy, no lock held across a round trip. The happy path costs a single
round trip; only genuine duplicates pay for the second.

Note that the violation branch returns a `Result`, not an exception — the SQL
exception is caught at the persistence boundary and never escapes it.

### 2. Statements stream end-to-end, or they are wrong

A 50,000-row statement must never be fully materialised — not in the server, not in
the client. This constrains every layer, and one `.ToListAsync()` anywhere silently
breaks it:

```
SQL Server  →  EF Core AsAsyncEnumerable() + AsNoTracking()
            →  repository returns IAsyncEnumerable<Movement>
            →  handler yields rows, accumulating the running total per row
            →  gRPC server streaming: await responseStream.WriteAsync(row)
            →  client reads one row at a time
```

The running total is accumulated in the handler as rows flow past — a single `decimal`
in memory, regardless of row count.

---

## Architecture

Clean Architecture, dependencies pointing inward only. Each project exposes public
interfaces plus one DI extension; everything else is `private sealed`.

```
src/
├─ CashMovements.Domain/          no dependencies
│   ├─ Movement, Account          sealed; readonly properties; guards throw
│   ├─ Result<T>, Error           expected failures as values
│   └─ AddDomain()
├─ CashMovements.Application/     depends on Domain
│   ├─ ports: IMovementRepository, IBalanceReader, IStatementReader
│   ├─ private sealed handlers    SubmitMovement, Transfer, GetBalance, StreamStatement
│   ├─ FluentValidation validators
│   └─ AddApplication()
├─ CashMovements.Persistence/     depends on Application + Domain
│   ├─ CashMovementsDbContext     composite-key configuration
│   ├─ private sealed repositories, incl. the 2627 catch
│   └─ AddPersistence(connectionString)
├─ CashMovements.Grpc/            .NET 8 host — composition root
│   ├─ Protos/cash_movements.proto
│   ├─ CashMovementsService       maps Result<T> → StatusCode
│   ├─ ErrorInterceptor           the global handler (ProblemDetails equivalent)
│   └─ Program.cs                 four AddX() calls, nothing else
└─ CashMovements.Wcf.Gateway/     .NET Framework 4.8 — built LAST
    └─ one NetTcp operation: GetBalance(accountId), as a gRPC client

tests/
├─ CashMovements.UnitTests/          domain guards, balance math, Result mapping
└─ CashMovements.IntegrationTests/   Testcontainers — real SQL Server
```

---

## Data model

```sql
Accounts
  AccountId      VARCHAR(10)  PK        -- ACC-001
  AccountName    VARCHAR(20)  NOT NULL
  ContactPerson  VARCHAR(30)
  CreatedDate    DATETIME2    NOT NULL
  CreatedBy      VARCHAR(50)

Movements
  AccountId      VARCHAR(10)  NOT NULL  -- ┐ composite PK
  ExternalRef    VARCHAR(20)  NOT NULL  -- ┘ (this IS the idempotency key)
  Currency       CHAR(3)      NOT NULL
  Amount         DECIMAL(19,4) NOT NULL -- +deposit / −withdrawal
  OccurredAt     DATETIME2    NOT NULL
  Narration      VARCHAR(200)
  RefNr          UNIQUEIDENTIFIER NOT NULL
  MovedBy        VARCHAR(50)
  MovedDate      DATETIME2    NOT NULL
  CONSTRAINT PK_Movements PRIMARY KEY (AccountId, ExternalRef)
  CONSTRAINT FK_Movements_Accounts FOREIGN KEY (AccountId) → Accounts

INDEX IX_Movements_Account_OccurredAt ON Movements (AccountId, OccurredAt)
      INCLUDE (Amount)   -- serves both the balance SUM and the statement scan
```

`DECIMAL(19,4)` never `float` — binary floating point cannot represent money exactly.
The covering index means the balance `SUM` and the statement range scan both read from
the index alone, without touching the table.

---

## gRPC contract

```protobuf
service CashMovements {
  rpc SubmitMovement (SubmitMovementRequest) returns (MovementResponse);
  rpc Transfer       (TransferRequest)       returns (TransferResponse);
  rpc GetBalance     (GetBalanceRequest)     returns (BalanceResponse);
  rpc StreamStatement(StatementRequest) returns (stream StatementRow);  // server streaming
}
```

Money crosses the wire as a **string** decimal (`"12500.00"`), not a `double` —
protobuf has no decimal type, and `double` would corrupt cents. Timestamps use
`google.protobuf.Timestamp` (UTC).

Error mapping — done once, in the interceptor and the service, and nowhere else:

| Situation | gRPC status |
|---|---|
| Validation failure | `INVALID_ARGUMENT` |
| Unknown account | `NOT_FOUND` |
| Same key, same fields | `OK` — returns the stored movement |
| Same key, different fields | `ALREADY_EXISTS` + the conflicting field names |
| Unexpected fault | `INTERNAL` (details logged, never leaked to the caller) |

---

## Build order

TDD per layer, as `planning.md` line 65 requires: write the failing test, watch it
fail for the right reason, implement the minimum that passes, refactor. Each step
leaves the solution compiling and green.

Steps 1–7 are the graded core; 8–10 are bonus and are sacrificed first if time runs short.

1. **Scaffold** — solution under `src/`, five projects, `.gitignore`, `git init`.
2. **Domain (TDD)** — tests first for the guard clauses: null account id throws,
   blank currency throws, zero amount throws. Then `Movement`, `Account`, `Result<T>`.
3. **Persistence schema** — DbContext, composite-key configuration, migration; SQL
   scripts for schema + seed data (the brief asks for these explicitly).
4. **Testcontainers harness** — a real SQL Server container, a fixture, one smoke test.
   *Built before the repository, so every step after this is verified against real SQL.*
5. **Repository + idempotency (TDD)** — **the centrepiece.** Write these tests first,
   and watch each one fail:
   - N parallel identical submits → exactly one row, balance equals one movement
   - conflicting resubmit → conflict `Result`, no second row written
   - identical resubmit → success, returns the stored row

   Then implement the insert/2627/compare flow until they pass.
6. **Handlers (TDD)** — submit, transfer (both legs, one transaction), balance,
   statement as `IAsyncEnumerable` with running total.
7. **gRPC host** — proto, service implementation, `ErrorInterceptor`,
   `Result<T>` → status mapping, server streaming. Integration test asserts a 50k-row
   statement streams without memory growth.
8. **Observability** — Serilog → Seq, OpenTelemetry → Jaeger.
9. **docker-compose** — SQL Server + Seq + Jaeger + the gRPC host; README run steps.
10. **WCF gateway** — .NET Framework 4.8, NetTcp, one `GetBalance` operation
    calling the gRPC host. *Windows-only; drop without regret if time is gone.*
11. **Docs** — `README.md` (how to run), `SOLUTION.md` (design and trade-offs), and the
    sequence / activity / data diagrams from `planning.md` §12.

`SOLUTION.md` must answer, because the brief asks directly: the conflict-handling
choice and why; the error approach; the simplifications made; the
aggregate-vs-materialised balance trade-off; and the `ProblemDetails` → gRPC status
substitution.

---

## Verification

The claim "it works" needs evidence at three levels.

**Correctness under concurrency** — the test that matters most:
```bash
dotnet test --filter FullyQualifiedName~Concurrent
```
Fires N simultaneous identical `SubmitMovement` calls at a real SQL Server container
and asserts exactly one row exists and the balance equals a single movement.

**Streaming holds under volume:**
```bash
dotnet test --filter FullyQualifiedName~Statement
```
Seeds 50,000 movements, streams them, asserts every row arrives, the running total is
correct, and process memory stays flat — proving nothing was materialised.

**End to end, by hand:**
```bash
docker compose up -d            # SQL Server, Seq, Jaeger, gRPC host
dotnet run --project src/CashMovements.Grpc
grpcurl -plaintext localhost:5001 list          # contract is discoverable
# submit a movement, submit it again → second returns the same stored row
# submit it a third time with a different amount → ALREADY_EXISTS
```
Then confirm traces appear in Jaeger (`localhost:16686`) and structured logs in Seq
(`localhost:5341`).

**Everything, before calling it done:**
```bash
dotnet build && dotnet test
```

---

## Risks

- **Testcontainers needs a running Docker daemon.** If Docker is unavailable, the
  integration suite cannot run — verify Docker works *before* step 4, not at step 7.
- **The WCF gateway is Windows-only** and mixes an SDK-style solution with a legacy
  `.csproj`. It is sequenced last precisely so this cannot block the core.
- **A stray `.ToListAsync()`** anywhere in the statement path silently defeats the
  streaming requirement while all tests still pass. The 50k memory test is the guard.
- **`private sealed` types cannot be tested directly from another assembly.** Test
  through the public interface (correct), or add `[assembly: InternalsVisibleTo]` and
  use `internal sealed` where a test genuinely needs the concrete type.
