# Frontend Full Scope Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build out the remaining `Sygnia.Frontend/Claude.md` scope on top of the existing homepage/Bootstrap 5 branch: gRPC-Web client services, Accounts/User/Statement components (statement paginated, PDF export), top nav, Sygnia branding, and the corresponding backend surface (CreateAccount/CreateUser RPCs, a new paged statement RPC).

**Architecture:** Angular 18 standalone components talk to `Sygnia.Presentation` over gRPC-Web (`@ng-grpc/grpc-web` client generated from the existing/extended `.proto` files, via `Grpc.AspNetCore.Web`). The already-merged `StreamStatement` RPC and its 50k-row streaming test are left untouched — a **new** `GetStatementPage` RPC is added alongside it for the UI's paginated table, per CLAUDE.md's rule that the streaming path is a graded invariant and must not be touched to serve an unrelated UI concern. Backend account/user creation reuses the CreateAccount/CreateUser MediatR handlers already merged on `docs/readme-and-solution` — this plan only adds the gRPC surface over them.

**Tech Stack:** .NET 8, EF Core, MediatR, Grpc.AspNetCore.Web; Angular 18, `grpc-web`, `google-protobuf`, Bootstrap 5, `jspdf` (client-side PDF), Angular Router, RxJS.

**Spec:** `src/Sygnia.Frontend/Claude.md` (in-scope items 1–10, all now in scope), root `CLAUDE.md` (streaming invariant, gRPC-Web requirement, status-code mapping).

## Global Constraints

- Money crosses the wire as a decimal **string**, never `double` (root CLAUDE.md).
- `StreamStatement` and its `AsAsyncEnumerable` path must not be modified (root CLAUDE.md invariant #2).
- No AutoMapper (`Sygnia.Presentation/CLAUDE.md`).
- gRPC-Web only for the browser: `UseGrpcWeb()` + `EnableGrpcWeb()` + CORS (root CLAUDE.md).
- Status mapping: validation → `INVALID_ARGUMENT`; unknown account → `NOT_FOUND`; duplicate same fields → `OK`; duplicate different fields → `ALREADY_EXISTS`; unexpected → `INTERNAL` (already implemented in `ResultExtensions`/`ErrorInterceptor` — reuse, don't reimplement).
- Every service gets an interface; MediatR handlers stay `internal sealed` (root CLAUDE.md coding standards) — applies to any new backend handler.
- TDD per layer: failing test → watch it fail → implement.

---

## File Structure

**Backend (new/modified):**
- `src/Sygnia.Backend/src/Sygnia.Presentation/Protos/accounts.proto` — new: `AccountService` (CreateAccount, GetAccount)
- `src/Sygnia.Backend/src/Sygnia.Presentation/Protos/users.proto` — new: `UserService` (CreateUser, GetUser)
- `src/Sygnia.Backend/src/Sygnia.Presentation/Protos/movements.proto` — modified: add `GetStatementPage` RPC + `GetStatementPageRequest`/`GetStatementPageResponse` messages (existing `GetStatement` untouched)
- `src/Sygnia.Backend/src/Sygnia.Presentation/Services/AccountGrpcService.cs` — new
- `src/Sygnia.Backend/src/Sygnia.Presentation/Services/UserGrpcService.cs` — new
- `src/Sygnia.Backend/src/Sygnia.Presentation/Services/MovementGrpcService.cs` — modified: add `GetStatementPage` handler
- `src/Sygnia.Backend/src/Sygnia.Presentation/Mapping/ProtoMapper.cs` — modified: add Account/User `ToProto()` extensions
- `src/Sygnia.Backend/src/Sygnia.Application/Queries/GetStatementPage/` — new: `GetStatementPageQuery`, `GetStatementPageQueryHandler`, `GetStatementPageQueryValidator`
- `src/Sygnia.Backend/src/Sygnia.Application/Interfaces/IStatementReader.cs` — modified: add `GetPageAsync(accountId, from, to, pageNumber, pageSize, ct)` returning `(IReadOnlyList<Movement> Rows, int TotalCount)`
- `src/Sygnia.Backend/src/Sygnia.Infrastructure/Repositories/StatementReader.cs` — modified: implement `GetPageAsync` with `Skip`/`Take` (NOT `AsAsyncEnumerable`, this path is intentionally buffered — it's a page, not the full stream)
- `src/Sygnia.Backend/src/Sygnia.Presentation/Program.cs` — modified: `MapGrpcService<AccountGrpcService>()`, `MapGrpcService<UserGrpcService>()`
- Test files mirroring each of the above under `src/Sygnia.Backend/tests/Sygnia.Tests/`

**Frontend (new/modified):**
- `src/Sygnia.Frontend/package.json` — add `google-protobuf`, `grpc-web`, `jspdf`, dev: `grpc-tools`, `protoc-gen-grpc-web` (or `ts-protoc-gen`)
- `src/Sygnia.Frontend/proto/` — copy of the 3 `.proto` files (symlink-free copy; keep in sync manually, note in README)
- `src/Sygnia.Frontend/src/app/grpc/` — generated `*_pb.js`/`*_grpc_web_pb.js` (checked in, regenerated via `npm run gen:proto`)
- `src/Sygnia.Frontend/src/app/services/movement.service.ts` — new: wraps `MovementServiceClient`
- `src/Sygnia.Frontend/src/app/services/account.service.ts` — new: wraps `AccountServiceClient`
- `src/Sygnia.Frontend/src/app/services/user.service.ts` — new: wraps `UserServiceClient`
- `src/Sygnia.Frontend/src/app/services/pdf-export.service.ts` — new: `jspdf`-based statement export
- `src/Sygnia.Frontend/src/app/accounts/accounts.component.ts` — new: create-account form + list
- `src/Sygnia.Frontend/src/app/user/user.component.ts` — new: submit movement / transfer / balance
- `src/Sygnia.Frontend/src/app/statement/statement.component.ts` — new: filter + paginated table
- `src/Sygnia.Frontend/src/app/statement/statement-preview/statement-preview.component.ts` — new
- `src/Sygnia.Frontend/src/app/nav/nav.component.ts` — new: top nav
- `src/Sygnia.Frontend/src/app/app.component.html` — modified: include `<app-nav>`
- `src/Sygnia.Frontend/src/app/app.routes.ts` — modified: add `/accounts`, `/user`, `/statement` routes
- `src/Sygnia.Frontend/src/index.html` — modified: `<title>Sygnia Cash Movements</title>`, favicon link
- `src/Sygnia.Frontend/public/favicon.ico` — replaced with Sygnia mark (user-supplied `image.png`, converted)

---

## Task 1: gRPC-Web plumbing on the backend (CORS + Grpc-Web middleware)

**Files:**
- Modify: `src/Sygnia.Backend/src/Sygnia.Presentation/Program.cs`
- Test: manual (grpc-web has no unit-testable middleware surface worth automating here)

**Interfaces:**
- Produces: `http://localhost:5000` accepts gRPC-Web requests (`application/grpc-web`) from `http://localhost:4200`.

- [ ] **Step 1:** Add `Grpc.AspNetCore.Web` package reference (check `Directory.Packages.props` first — add a `PackageVersion` entry if missing, matching the installed `Grpc.AspNetCore` major version).
- [ ] **Step 2:** In `Program.cs`, add:
  ```csharp
  builder.Services.AddCors(o => o.AddPolicy("frontend", p => p
      .WithOrigins("http://localhost:4200")
      .AllowAnyHeader()
      .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding")));
  // ...
  app.UseCors("frontend");
  app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });
  ```
  and append `.EnableGrpcWeb()` to each `app.MapGrpcService<T>()` call.
- [ ] **Step 3:** Run backend (`dotnet run --project src/Sygnia.Presentation`), confirm it still starts and existing gRPC-Web-agnostic tests still pass (`dotnet test --filter "FullyQualifiedName!~Integration"`).
- [ ] **Step 4: Commit**
  ```bash
  git add src/Sygnia.Backend/src/Sygnia.Presentation/Program.cs src/Sygnia.Backend/src/Sygnia.Presentation/Sygnia.Presentation.csproj src/Sygnia.Backend/Directory.Packages.props
  git commit -m "Enable gRPC-Web + CORS for the Angular origin"
  ```

## Task 2: AccountService proto + gRPC service (CreateAccount, GetAccount)

**Files:**
- Create: `src/Sygnia.Backend/src/Sygnia.Presentation/Protos/accounts.proto`
- Create: `src/Sygnia.Backend/src/Sygnia.Presentation/Services/AccountGrpcService.cs`
- Modify: `src/Sygnia.Backend/src/Sygnia.Presentation/Mapping/ProtoMapper.cs` (add `Account.ToProto()`)
- Modify: `src/Sygnia.Backend/src/Sygnia.Presentation/Program.cs` (register the .proto in the `.csproj` `<Protobuf>` item group, `MapGrpcService<AccountGrpcService>().EnableGrpcWeb()`)
- Test: `src/Sygnia.Backend/tests/Sygnia.Tests/Presentation/AccountGrpcServiceTests.cs`

**Interfaces:**
- Consumes: `CreateAccountCommand`/`CreateAccountCommandHandler` (already merged on `docs/readme-and-solution`), `IAccountRepository.GetAsync`.
- Produces: `AccountService.AccountServiceBase` with `CreateAccount(CreateAccountRequest) -> AccountProto`, `GetAccount(GetAccountRequest) -> AccountProto`, consumed by Task 8's Angular `AccountService`.

- [ ] **Step 1:** Write `accounts.proto`:
  ```protobuf
  syntax = "proto3";
  import "google/protobuf/timestamp.proto";
  option csharp_namespace = "Sygnia.Presentation";
  package accounts;

  service AccountService {
    rpc CreateAccount(CreateAccountRequest) returns (Account);
    rpc GetAccount(GetAccountRequest) returns (Account);
  }

  message Account {
    string account_id = 1;
    string account_name = 2;
    string contact_person = 3;
    string currency = 4;
    google.protobuf.Timestamp created_date = 5;
    string created_by = 6;
  }

  message CreateAccountRequest {
    string account_id = 1;
    string account_name = 2;
    string contact_person = 3;
    string currency = 4;
    string created_by = 5;
  }

  message GetAccountRequest {
    string account_id = 1;
  }
  ```
- [ ] **Step 2:** Add `<Protobuf Include="Protos\accounts.proto" GrpcServices="Server" />` to `Sygnia.Presentation.csproj` next to the existing `movements.proto` entry.
- [ ] **Step 3:** Add to `ProtoMapper.cs`:
  ```csharp
  public static Account ToProto(this Sygnia.Domain.Models.Account account) => new()
  {
      AccountId = account.AccountId,
      AccountName = account.AccountName,
      ContactPerson = account.ContactPerson ?? string.Empty,
      Currency = account.Currency,
      CreatedDate = Timestamp.FromDateTime(account.CreatedDate),
      CreatedBy = account.CreatedBy,
  };
  ```
- [ ] **Step 4:** Write the failing test `AccountGrpcServiceTests.cs`:
  ```csharp
  public sealed class AccountGrpcServiceTests
  {
      [Fact]
      public async Task CreateAccount_Valid_ReturnsAccountProto()
      {
          var mediator = Substitute.For<IMediator>();
          mediator.Send(Arg.Any<CreateAccountCommand>(), Arg.Any<CancellationToken>())
              .Returns(Result<Account>.Success(new Account("ACC-001", "Test", null, "ZAR", DateTime.UtcNow, "seed")));
          var service = new AccountGrpcService(mediator);

          var response = await service.CreateAccount(
              new CreateAccountRequest { AccountId = "ACC-001", AccountName = "Test", Currency = "ZAR", CreatedBy = "seed" },
              TestServerCallContext.Create());

          Assert.Equal("ACC-001", response.AccountId);
      }

      [Fact]
      public async Task CreateAccount_DuplicateId_ThrowsAlreadyExists()
      {
          var mediator = Substitute.For<IMediator>();
          mediator.Send(Arg.Any<CreateAccountCommand>(), Arg.Any<CancellationToken>())
              .Returns(Result<Account>.Failure(new Error("account.already_exists", "exists")));
          var service = new AccountGrpcService(mediator);

          var ex = await Assert.ThrowsAsync<RpcException>(() => service.CreateAccount(
              new CreateAccountRequest { AccountId = "ACC-001", AccountName = "Test", Currency = "ZAR", CreatedBy = "seed" },
              TestServerCallContext.Create()));

          Assert.Equal(StatusCode.AlreadyExists, ex.StatusCode);
      }
  }
  ```
  Check whether `TestServerCallContext` already exists under `Sygnia.Tests/Presentation` from the movements test suite — reuse it; if absent, check how `MovementGrpcService`'s existing tests construct `ServerCallContext` and mirror that exactly.
- [ ] **Step 5:** Run: `dotnet test --filter FullyQualifiedName~AccountGrpcServiceTests` — expect FAIL (`AccountGrpcService` doesn't exist).
- [ ] **Step 6:** Implement `AccountGrpcService.cs`, mirroring `MovementGrpcService`'s shape:
  ```csharp
  internal sealed class AccountGrpcService(IMediator mediator) : AccountService.AccountServiceBase
  {
      public override async Task<Account> CreateAccount(CreateAccountRequest request, ServerCallContext context)
      {
          var command = new CreateAccountCommand(
              request.AccountId, request.AccountName,
              string.IsNullOrEmpty(request.ContactPerson) ? null : request.ContactPerson,
              request.Currency, request.CreatedBy);

          var result = await mediator.Send(command, context.CancellationToken);
          return result.IsSuccess ? result.Value.ToProto() : throw result.Error.ToRpcException();
      }

      public override async Task<Account> GetAccount(GetAccountRequest request, ServerCallContext context)
      {
          // No existing query for a single account read — add IAccountRepository call via a
          // small ad-hoc MediatR query GetAccountQuery/GetAccountQueryHandler mirroring
          // GetBalanceQueryHandler's shape (inject IAccountRepository, return NOT_FOUND Result).
      }
  }
  ```
  For `GetAccount`, first add `src/Sygnia.Backend/src/Sygnia.Application/Queries/GetAccount/GetAccountQuery.cs` + handler (TDD: write `GetAccountQueryHandlerTests.cs` first, using `FakeAccountRepository` from Task testing infra already on `docs/readme-and-solution`).
- [ ] **Step 7:** Run: `dotnet test --filter FullyQualifiedName~AccountGrpcServiceTests` — expect PASS.
- [ ] **Step 8:** Register in `Program.cs`: `app.MapGrpcService<AccountGrpcService>().EnableGrpcWeb();`
- [ ] **Step 9: Commit**
  ```bash
  git add src/Sygnia.Backend/src/Sygnia.Presentation/Protos/accounts.proto src/Sygnia.Backend/src/Sygnia.Presentation/Services/AccountGrpcService.cs src/Sygnia.Backend/src/Sygnia.Presentation/Mapping/ProtoMapper.cs src/Sygnia.Backend/src/Sygnia.Presentation/Program.cs src/Sygnia.Backend/src/Sygnia.Application/Queries/GetAccount src/Sygnia.Backend/tests/Sygnia.Tests/Presentation/AccountGrpcServiceTests.cs src/Sygnia.Backend/tests/Sygnia.Tests/Application/GetAccountQueryHandlerTests.cs
  git commit -m "Add AccountService gRPC surface (CreateAccount, GetAccount)"
  ```

## Task 3: UserService proto + gRPC service (CreateUser, GetUser)

**Files:** mirrors Task 2 exactly, substituting User for Account.
- Create: `src/Sygnia.Backend/src/Sygnia.Presentation/Protos/users.proto`
- Create: `src/Sygnia.Backend/src/Sygnia.Presentation/Services/UserGrpcService.cs`
- Create: `src/Sygnia.Backend/src/Sygnia.Application/Queries/GetUser/` (query + handler)
- Test: `src/Sygnia.Backend/tests/Sygnia.Tests/Presentation/UserGrpcServiceTests.cs`, `GetUserQueryHandlerTests.cs`

**Interfaces:**
- Consumes: `CreateUserCommand`/`CreateUserCommandHandler` (merged), `IUserRepository.GetAsync`.
- Produces: `UserService.UserServiceBase` with `CreateUser`, `GetUser`, consumed by Task 8's Angular `UserService`.

- [ ] **Step 1:** `users.proto`:
  ```protobuf
  syntax = "proto3";
  option csharp_namespace = "Sygnia.Presentation";
  package users;

  service UserService {
    rpc CreateUser(CreateUserRequest) returns (User);
    rpc GetUser(GetUserRequest) returns (User);
  }

  message User {
    string id = 1;
    string name = 2;
    string surname = 3;
  }

  message CreateUserRequest {
    string id = 1;
    string name = 2;
    string surname = 3;
  }

  message GetUserRequest {
    string id = 1;
  }
  ```
- [ ] **Step 2:** Add `<Protobuf Include="Protos\users.proto" GrpcServices="Server" />`.
- [ ] **Step 3:** Add `ProtoMapper` extension `User.ToProto()`.
- [ ] **Step 4–7:** Same TDD cycle as Task 2 (`GetUserQueryHandlerTests` → `GetUserQueryHandler`, `UserGrpcServiceTests` → `UserGrpcService`).
- [ ] **Step 8:** Register: `app.MapGrpcService<UserGrpcService>().EnableGrpcWeb();`
- [ ] **Step 9: Commit**
  ```bash
  git add src/Sygnia.Backend/src/Sygnia.Presentation/Protos/users.proto src/Sygnia.Backend/src/Sygnia.Presentation/Services/UserGrpcService.cs src/Sygnia.Backend/src/Sygnia.Application/Queries/GetUser src/Sygnia.Backend/src/Sygnia.Presentation/Program.cs src/Sygnia.Backend/tests/Sygnia.Tests/Presentation/UserGrpcServiceTests.cs src/Sygnia.Backend/tests/Sygnia.Tests/Application/GetUserQueryHandlerTests.cs
  git commit -m "Add UserService gRPC surface (CreateUser, GetUser)"
  ```

## Task 4: Paged statement RPC (leaves StreamStatement untouched)

**Files:**
- Create: `src/Sygnia.Backend/src/Sygnia.Application/Queries/GetStatementPage/GetStatementPageQuery.cs`
- Create: `src/Sygnia.Backend/src/Sygnia.Application/Queries/GetStatementPage/GetStatementPageQueryHandler.cs`
- Create: `src/Sygnia.Backend/src/Sygnia.Application/Queries/GetStatementPage/GetStatementPageQueryValidator.cs`
- Modify: `src/Sygnia.Backend/src/Sygnia.Application/Interfaces/IStatementReader.cs`
- Modify: `src/Sygnia.Backend/src/Sygnia.Infrastructure/Repositories/StatementReader.cs`
- Modify: `src/Sygnia.Backend/src/Sygnia.Presentation/Protos/movements.proto` (append `GetStatementPage` RPC — do not touch `GetStatement`)
- Modify: `src/Sygnia.Backend/src/Sygnia.Presentation/Services/MovementGrpcService.cs` (append handler)
- Test: `GetStatementPageQueryHandlerTests.cs` (unit, `FakeStatementReader`), `StatementReaderTests.cs` (integration, add one `GetPageAsync` case to the existing Testcontainers fixture — do not touch the existing 50k streaming test)

**Interfaces:**
- Consumes: `IStatementReader` (existing), the `IX_Movements_Account_OccurredAt` index (existing — `Skip`/`Take` on `AccountId`+`OccurredAt` still uses it).
- Produces: `IStatementReader.GetPageAsync(string accountId, DateTime from, DateTime to, int pageNumber, int pageSize, CancellationToken ct) -> Task<(IReadOnlyList<Movement> Rows, int TotalCount)>`, consumed by Task 9's `StatementComponent`.

- [ ] **Step 1:** Add to `IStatementReader.cs`:
  ```csharp
  /// <summary>
  /// A single page for the UI's paginated table — buffered on purpose, unlike
  /// <see cref="GetStatementAsync"/>'s full stream. Never used for the 50k-row memory test.
  /// </summary>
  Task<(IReadOnlyList<Movement> Rows, int TotalCount)> GetPageAsync(
      string accountId, DateTime from, DateTime to, int pageNumber, int pageSize, CancellationToken cancellationToken);
  ```
- [ ] **Step 2:** Write failing test in `Sygnia.Tests/Application/GetStatementPageQueryHandlerTests.cs` against a `FakeStatementReader.GetPageAsync` (extend the existing fake), asserting: page 1 of 2 with pageSize 2 over 3 seeded movements returns 2 rows and `TotalCount == 3`.
- [ ] **Step 3:** Run: `dotnet test --filter FullyQualifiedName~GetStatementPageQueryHandlerTests` — FAIL.
- [ ] **Step 4:** Implement `GetStatementPageQuery(string AccountId, DateTime From, DateTime To, int PageNumber, int PageSize) : IRequest<Result<StatementPage>>` where `StatementPage` is a small new `Sygnia.Application` record `(IReadOnlyList<Movement> Rows, int TotalCount)`. Handler validates account exists (`IAccountRepository.GetAsync`) the same way `GetStatementQueryHandler` does, then delegates to `IStatementReader.GetPageAsync`.
- [ ] **Step 5:** Run: PASS.
- [ ] **Step 6:** Implement `StatementReader.GetPageAsync` in Infrastructure:
  ```csharp
  public async Task<(IReadOnlyList<Movement> Rows, int TotalCount)> GetPageAsync(
      string accountId, DateTime from, DateTime to, int pageNumber, int pageSize, CancellationToken cancellationToken)
  {
      var query = db.Movements.AsNoTracking()
          .Where(m => m.AccountId == accountId && m.OccurredAt >= from && m.OccurredAt <= to);

      var total = await query.CountAsync(cancellationToken);
      var rows = await query
          .OrderBy(m => m.OccurredAt)
          .Skip((pageNumber - 1) * pageSize)
          .Take(pageSize)
          .Select(m => m.ToDomain())
          .ToListAsync(cancellationToken);

      return (rows, total);
  }
  ```
  This is the one intentional `ToListAsync` in the statement path — scoped to a single page, not the full result set, so it does not defeat the streaming requirement (which `GetStatement`/`StreamStatement` alone must satisfy).
- [ ] **Step 7:** Append to `movements.proto` (do not touch existing `GetStatement`/`StatementLine`):
  ```protobuf
  rpc GetStatementPage(GetStatementPageRequest) returns (GetStatementPageResponse);

  message GetStatementPageRequest {
    string account_id = 1;
    google.protobuf.Timestamp from = 2;
    google.protobuf.Timestamp to = 3;
    int32 page_number = 4;
    int32 page_size = 5;
  }

  message GetStatementPageResponse {
    repeated StatementLine lines = 1;
    int32 total_count = 2;
  }
  ```
- [ ] **Step 8:** Append `GetStatementPage` handler to `MovementGrpcService.cs`, following the same `mediator.Send` → `Result` → proto pattern as `GetBalance`.
- [ ] **Step 9:** Add one integration test to `StatementReaderTests.cs` (same Testcontainers fixture as the existing tests) verifying `GetPageAsync` pagination math against a small seeded set — leave the existing 50k `AsAsyncEnumerable` test file untouched.
- [ ] **Step 10:** Run full suite: `dotnet test --filter "FullyQualifiedName!~Integration"` then (if Docker is up) the integration filter — confirm the 50k streaming test still passes unmodified.
- [ ] **Step 11: Commit**
  ```bash
  git add src/Sygnia.Backend/src/Sygnia.Application/Queries/GetStatementPage src/Sygnia.Backend/src/Sygnia.Application/Interfaces/IStatementReader.cs src/Sygnia.Backend/src/Sygnia.Infrastructure/Repositories/StatementReader.cs src/Sygnia.Backend/src/Sygnia.Presentation/Protos/movements.proto src/Sygnia.Backend/src/Sygnia.Presentation/Services/MovementGrpcService.cs src/Sygnia.Backend/tests/Sygnia.Tests/Application/GetStatementPageQueryHandlerTests.cs src/Sygnia.Backend/tests/Sygnia.Tests/Infrastructure/Integration/StatementReaderTests.cs
  git commit -m "Add GetStatementPage RPC for the paginated statement UI, StreamStatement untouched"
  ```

## Task 5: gRPC-Web codegen pipeline for Angular

**Files:**
- Modify: `src/Sygnia.Frontend/package.json`
- Create: `src/Sygnia.Frontend/proto/movements.proto`, `accounts.proto`, `users.proto` (copied verbatim from `Sygnia.Presentation/Protos/`)
- Create: `src/Sygnia.Frontend/scripts/gen-proto.sh` (or `.ps1` — Windows dev machine, prefer `.ps1` with an npm script wrapper)
- Modify: `.gitignore` if generated JS should NOT be checked in (decide: check in, since there's no CI step that runs protoc yet — simpler for reviewers to `npm ci && npm start` without protoc installed)

**Interfaces:**
- Produces: `src/Sygnia.Frontend/src/app/grpc/movements_pb.js`, `movements_grpc_web_pb.js` (and same for accounts/users) — the generated client stubs Tasks 6–8 import.

- [ ] **Step 1:** `npm install --save grpc-web google-protobuf` and `npm install --save-dev grpc-tools ts-protoc-gen`.
- [ ] **Step 2:** Copy the three `.proto` files from `Sygnia.Presentation/Protos/` into `src/Sygnia.Frontend/proto/` verbatim (byte-for-byte — no edits; the backend is the source of truth).
- [ ] **Step 3:** Add npm script:
  ```json
  "gen:proto": "grpc_tools_node_protoc --js_out=import_style=commonjs,binary:src/app/grpc --grpc-web_out=import_style=typescript,mode=grpcwebtext:src/app/grpc -I proto proto/movements.proto proto/accounts.proto proto/users.proto"
  ```
  (Requires `protoc-gen-grpc-web` plugin binary on PATH — document the install step in `src/Sygnia.Frontend/README.md`: download from the `grpc-web` GitHub releases page for Windows, or use the `protoc-gen-grpc-web` npm-distributed binary if available for the platform.)
- [ ] **Step 4:** Run `npm run gen:proto`, confirm `src/app/grpc/movements_pb.js`, `movements_grpc_web_pb.ts`, `accounts_pb.js`, `accounts_grpc_web_pb.ts`, `users_pb.js`, `users_grpc_web_pb.ts` are created.
- [ ] **Step 5:** `npx ng build` — confirm the generated files compile cleanly with the existing `tsconfig.app.json` (may need `"allowJs": true` added since the `_pb.js` files are plain JS — add it if the build fails on that).
- [ ] **Step 6: Commit**
  ```bash
  git add src/Sygnia.Frontend/package.json src/Sygnia.Frontend/package-lock.json src/Sygnia.Frontend/proto src/Sygnia.Frontend/scripts/gen-proto.sh src/Sygnia.Frontend/src/app/grpc src/Sygnia.Frontend/tsconfig.app.json src/Sygnia.Frontend/README.md
  git commit -m "Add gRPC-Web codegen pipeline and generated client stubs"
  ```

## Task 6: Angular MovementService (wraps generated MovementServiceClient)

**Files:**
- Create: `src/Sygnia.Frontend/src/app/services/movement.service.ts`
- Test: `src/Sygnia.Frontend/src/app/services/movement.service.spec.ts`

**Interfaces:**
- Consumes: `MovementServiceClient` from `src/app/grpc/movements_grpc_web_pb.ts`, backend `http://localhost:5000`.
- Produces: `MovementService` injectable with `submitMovement(...)`, `transfer(...)`, `getBalance(accountId): Observable<{accountId: string; balance: string}>`, `getStatementPage(...): Observable<{lines: StatementLineDto[]; totalCount: number}>` — consumed by Task 10 (`UserComponent`) and Task 9 (`StatementComponent`).

- [ ] **Step 1:** Write failing spec asserting `getBalance` wraps the client's promise-based call in an `Observable` and maps the proto response to a plain DTO:
  ```typescript
  it('maps GetBalanceResponse to a plain DTO', async () => {
    const client = jasmine.createSpyObj('MovementServiceClient', ['getBalance']);
    const proto = new GetBalanceResponse();
    proto.setAccountId('ACC-001');
    proto.setBalance('100.00');
    client.getBalance.and.callFake((_req: unknown, _meta: unknown, cb: Function) => cb(null, proto));
    const service = new MovementService(client);

    const result = await firstValueFrom(service.getBalance('ACC-001'));

    expect(result).toEqual({ accountId: 'ACC-001', balance: '100.00' });
  });
  ```
- [ ] **Step 2:** Run: `npx ng test --watch=false --browsers=ChromeHeadless --include='**/movement.service.spec.ts'` — FAIL.
- [ ] **Step 3:** Implement `movement.service.ts`:
  ```typescript
  import { Injectable } from '@angular/core';
  import { Observable } from 'rxjs';
  import { MovementServiceClient } from '../grpc/MovementsServiceClientPb';
  import { GetBalanceRequest, SubmitMovementRequest, TransferRequest, GetStatementPageRequest } from '../grpc/movements_pb';

  export interface BalanceDto { accountId: string; balance: string; }

  @Injectable({ providedIn: 'root' })
  export class MovementService {
    private readonly client = new MovementServiceClient('http://localhost:5000');

    getBalance(accountId: string): Observable<BalanceDto> {
      return new Observable(observer => {
        const req = new GetBalanceRequest();
        req.setAccountId(accountId);
        this.client.getBalance(req, {}, (err, res) => {
          if (err) { observer.error(err); return; }
          observer.next({ accountId: res.getAccountId(), balance: res.getBalance() });
          observer.complete();
        });
      });
    }
    // submitMovement, transfer, getStatementPage follow the same next/error/complete shape
  }
  ```
  Note the constructor takes the client so the spec above can inject a spy — add a constructor parameter `constructor(private readonly client: MovementServiceClient = new MovementServiceClient('http://localhost:5000'))`.
- [ ] **Step 4:** Run: PASS. Fill in `submitMovement`, `transfer`, `getStatementPage` the same way, with matching specs for each (same next/error/complete pattern — one spec per method, following Step 1's shape).
- [ ] **Step 5: Commit**
  ```bash
  git add src/Sygnia.Frontend/src/app/services/movement.service.ts src/Sygnia.Frontend/src/app/services/movement.service.spec.ts
  git commit -m "Add Angular MovementService wrapping the generated gRPC-Web client"
  ```

## Task 7: Angular AccountService + UserService

**Files:**
- Create: `src/Sygnia.Frontend/src/app/services/account.service.ts` (+ spec)
- Create: `src/Sygnia.Frontend/src/app/services/user.service.ts` (+ spec)

**Interfaces:**
- Produces: `AccountService.createAccount(...)`, `AccountService.getAccount(id)`; `UserService.createUser(...)`, `UserService.getUser(id)` — consumed by Task 8 (`AccountsComponent`) and Task 10 (`UserComponent`, for the mover dropdown).

- [ ] **Step 1–5:** Same TDD shape as Task 6, one service each, wrapping `AccountServiceClient` and `UserServiceClient` from the generated stubs.
- [ ] **Step 6: Commit**
  ```bash
  git add src/Sygnia.Frontend/src/app/services/account.service.ts src/Sygnia.Frontend/src/app/services/account.service.spec.ts src/Sygnia.Frontend/src/app/services/user.service.ts src/Sygnia.Frontend/src/app/services/user.service.spec.ts
  git commit -m "Add Angular AccountService and UserService"
  ```

## Task 8: AccountsComponent (create account form + result display)

**Files:**
- Create: `src/Sygnia.Frontend/src/app/accounts/accounts.component.ts`, `.html`, `.scss`, `.spec.ts`
- Modify: `src/Sygnia.Frontend/src/app/app.routes.ts` (add `{ path: 'accounts', component: AccountsComponent }`)

**Interfaces:**
- Consumes: `AccountService.createAccount(accountId, accountName, contactPerson, currency, createdBy): Observable<AccountDto>` (Task 7).

- [ ] **Step 1:** Write failing spec: submitting the form with valid values calls `AccountService.createAccount` with the form values and renders the returned account id on success.
- [ ] **Step 2:** Run — FAIL (component doesn't exist).
- [ ] **Step 3:** Implement a reactive form (`FormBuilder`) with `accountId`, `accountName`, `contactPerson`, `currency`, `createdBy` fields, Bootstrap `form-control`/`form-label` classes, calling `accountService.createAccount(...)` on submit and showing either the created account or the mapped gRPC error message (`error.message` from the `grpc-web` error shape).
- [ ] **Step 4:** Run — PASS.
- [ ] **Step 5:** Add route, verify `/accounts` renders via `agent-browser` or manual browser check against the running dev server.
- [ ] **Step 6: Commit**
  ```bash
  git add src/Sygnia.Frontend/src/app/accounts src/Sygnia.Frontend/src/app/app.routes.ts
  git commit -m "Add AccountsComponent for account creation"
  ```

## Task 9: StatementComponent (filter, pagination, preview, PDF download)

**Files:**
- Create: `src/Sygnia.Frontend/src/app/statement/statement.component.ts`, `.html`, `.scss`, `.spec.ts`
- Create: `src/Sygnia.Frontend/src/app/statement/statement-preview/statement-preview.component.ts`, `.html`, `.scss`, `.spec.ts`
- Create: `src/Sygnia.Frontend/src/app/services/pdf-export.service.ts`, `.spec.ts`
- Modify: `src/Sygnia.Frontend/package.json` (`jspdf`, `jspdf-autotable`)
- Modify: `src/Sygnia.Frontend/src/app/app.routes.ts`

**Interfaces:**
- Consumes: `MovementService.getStatementPage(accountId, from, to, pageNumber, pageSize): Observable<{lines, totalCount}>` (Task 6, backed by Task 4's `GetStatementPage` RPC).
- Produces: `PdfExportService.exportStatement(lines: StatementLineDto[]): void` (triggers a browser download).

- [ ] **Step 1:** Write failing spec for `StatementComponent`: setting account id + date range and calling `search()` invokes `movementService.getStatementPage` with page 1, and rendered rows equal the mocked response's `lines`.
- [ ] **Step 2:** Run — FAIL.
- [ ] **Step 3:** Implement filter form (account id select fed by `AccountService`, from/to date inputs) + Bootstrap `table` bound to the current page's rows, plus a `nav` pagination control (`ngb`-free, plain Bootstrap `pagination` markup driven by `totalCount`/`pageSize`).
- [ ] **Step 4:** Run — PASS.
- [ ] **Step 5:** Write failing spec for `PdfExportService.exportStatement`: asserts `jsPDF.prototype.save` is called once. Implement using `jspdf` + `jspdf-autotable` to render a simple movements table.
- [ ] **Step 6:** Run — PASS.
- [ ] **Step 7:** Write failing spec for `StatementPreviewComponent`: given an `@Input() lines`, renders one row per line plus a running total column; add a "Download PDF" button wired to `PdfExportService`.
- [ ] **Step 8:** Run — PASS. Wire `StatementPreviewComponent` into `StatementComponent`'s template.
- [ ] **Step 9:** Add route, manual/agent-browser check of `/statement` against a seeded account (use `scripts/03_seed_statement_50000.sql` account `ACC-001` to sanity-check pagination doesn't try to render all 50k rows client-side — confirm only `pageSize` rows are ever in the DOM).
- [ ] **Step 10: Commit**
  ```bash
  git add src/Sygnia.Frontend/src/app/statement src/Sygnia.Frontend/src/app/services/pdf-export.service.ts src/Sygnia.Frontend/src/app/services/pdf-export.service.spec.ts src/Sygnia.Frontend/package.json src/Sygnia.Frontend/package-lock.json src/Sygnia.Frontend/src/app/app.routes.ts
  git commit -m "Add paginated StatementComponent with preview and PDF export"
  ```

## Task 10: UserComponent (submit movement / transfer / balance)

**Files:**
- Create: `src/Sygnia.Frontend/src/app/user/user.component.ts`, `.html`, `.scss`, `.spec.ts`
- Modify: `src/Sygnia.Frontend/src/app/app.routes.ts`

**Interfaces:**
- Consumes: `MovementService.submitMovement(...)`, `MovementService.transfer(...)`, `MovementService.getBalance(...)` (Task 6); `UserService.getUser(id)` (Task 7, for identifying the acting user).

- [ ] **Step 1:** Write failing spec: submitting the "submit movement" sub-form calls `movementService.submitMovement` with mapped values.
- [ ] **Step 2:** Run — FAIL.
- [ ] **Step 3:** Implement three Bootstrap `nav-tabs` panes (Submit Movement / Transfer / Balance), each its own reactive form, sharing one component. Amount inputs are plain text (not `type=number`, to preserve exact decimal strings crossing the wire per root CLAUDE.md).
- [ ] **Step 4:** Run — PASS. Add specs + verify for `transfer` and `getBalance` panes the same way.
- [ ] **Step 5:** Add route, manual check.
- [ ] **Step 6: Commit**
  ```bash
  git add src/Sygnia.Frontend/src/app/user src/Sygnia.Frontend/src/app/app.routes.ts
  git commit -m "Add UserComponent for submit/transfer/balance"
  ```

## Task 11: Top nav + Sygnia branding + tab title

**Files:**
- Create: `src/Sygnia.Frontend/src/app/nav/nav.component.ts`, `.html`, `.scss`, `.spec.ts`
- Modify: `src/Sygnia.Frontend/src/app/app.component.html` (add `<app-nav>` above `<router-outlet>`)
- Modify: `src/Sygnia.Frontend/src/index.html` (`<title>`, favicon `<link>`)
- Modify/Create: `src/Sygnia.Frontend/public/favicon.ico` (derived from the user-supplied `src/Sygnia.Frontend/image.png`)

**Interfaces:**
- Consumes: Angular `RouterLink`/`routerLinkActive` against the routes added in Tasks 8–10.

- [ ] **Step 1:** Write failing spec for `NavComponent`: renders 4 `routerLink`s — Home, Accounts, User, Statement.
- [ ] **Step 2:** Run — FAIL.
- [ ] **Step 3:** Implement a Bootstrap `navbar navbar-expand-lg` with a brand `<img>` pointing at `/favicon.ico` (or a dedicated `assets/sygnia-logo.png` if the source image has enough resolution — check `image.png`'s dimensions first; if it's icon-sized, use it only for the favicon and ask the user for a full logo asset for the brand mark rather than upscaling it) and `routerLink`/`routerLinkActive="active"` nav items for Home/Accounts/User/Statement.
- [ ] **Step 4:** Run — PASS.
- [ ] **Step 5:** Convert `image.png` to `favicon.ico` (`public/favicon.ico`), update `index.html`'s `<link rel="icon">` and set `<title>Sygnia Cash Movements</title>` (matches the tab-header ask in `Claude.md` item 10).
- [ ] **Step 6:** Wire `<app-nav>` into `app.component.html` above `<router-outlet>`.
- [ ] **Step 7:** Manual/agent-browser screenshot check of the full shell (nav + home) to confirm branding renders.
- [ ] **Step 8: Commit**
  ```bash
  git add src/Sygnia.Frontend/src/app/nav src/Sygnia.Frontend/src/app/app.component.html src/Sygnia.Frontend/src/index.html src/Sygnia.Frontend/public/favicon.ico
  git commit -m "Add top nav, Sygnia branding, and browser tab title"
  ```

---

## Self-Review Notes

- **Spec coverage:** item 1 (Angular app) — already done on this branch pre-plan; item 2 (homepage) — done; item 3 (Bootstrap 5) — done; item 4 (accounts component) — Task 8; item 5 (user component) — Task 10; item 6 (statement, pagination, backend pagination, preview, PDF) — Tasks 4 + 9; item 7 (gRPC services) — Tasks 1–3, 5–7; item 8 (logo/favicon) — Task 11; item 9 (top nav) — Task 11; item 10 (tab title) — Task 11.
- **Streaming invariant:** Task 4 explicitly adds a new RPC/method rather than touching `GetStatement`/`StreamStatement`/`AsAsyncEnumerable` — verified by leaving the 50k integration test file unmodified and adding a separate assertion.
- **No AutoMapper:** all mapping in Tasks 2–4 is hand-written extension methods, consistent with `Sygnia.Presentation/CLAUDE.md`.
- **Open question carried into Task 11, Step 3:** whether `image.png` is a usable full-size logo or icon-only — flagged for a quick check-in rather than guessing, since upscaling a small image would look bad and there's no way to know its resolution without opening it first.
