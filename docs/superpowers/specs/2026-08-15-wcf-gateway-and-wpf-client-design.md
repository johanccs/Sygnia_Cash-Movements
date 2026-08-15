# WCF Gateway + WPF Client — Design

## Context

Two additions to `docs/project-scaffold-done.md`'s "Additions" section (12, 13), both matching optional
items already named in the assignment brief:

- **Task 3 (optional):** a minimal legacy WCF NetTcp gateway exposing one `GetBalance` operation,
  runnable alongside the gRPC host.
- **Optional extension:** a small client utility that consumes the balance/statement API.

Both were previously marked `MISSING (optional)` in the implementation-vs-brief audit. This spec
covers the two projects needed to close that gap: `Sygnia.Wcf.Gateway` and `Sygnia.WpfClient`.

## Architecture

```
Sygnia.WpfClient (.NET Framework 4.8, WPF)
        │  NetTcpBinding, ChannelFactory<IBalanceService>
        ▼
Sygnia.Wcf.Gateway (.NET Framework 4.8, self-hosted console)
        │  Grpc.Net.Client + WinHttpHandler (HTTP/2 over .NET Framework)
        ▼
Sygnia.Presentation gRPC host (.NET 8)  →  GetBalance RPC  →  existing balance pipeline
```

The gateway takes no dependency on `Sygnia.Infrastructure`/`Sygnia.Application` — it is a thin
protocol translator, consistent with CLAUDE.md's framing of it as "a `.NET Framework 4.8` WCF
gateway (NetTcp, one `GetBalance` operation acting as a gRPC client)". This guarantees the WCF
and gRPC paths return identical balances for identical data, satisfying the brief's "new and
legacy entry points... produce consistent outcomes" requirement — there is now a second entry
point to be consistent with.

## Component 1: Sygnia.Wcf.Gateway

**Location:** `src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/` (already named in the CLAUDE.md
architecture diagram, "built LAST").

**Project:** .NET Framework 4.8, classic `.csproj` (not SDK-style — WCF server hosting isn't
supported on SDK-style without extra tooling). This is the one project in the solution that
can't share `Directory.Build.props`/`Directory.Packages.props` centralization, per CLAUDE.md's
"Known traps" section.

**Contract:**
```csharp
[ServiceContract]
public interface IBalanceService
{
    [OperationContract]
    BalanceResponse GetBalance(string accountId);
}

[DataContract]
public class BalanceResponse
{
    [DataMember] public string AccountId { get; set; }
    [DataMember] public string Currency { get; set; }
    [DataMember] public string Balance { get; set; }   // decimal-as-string, same convention as gRPC wire format
}
```

**Hosting:** self-hosted console app. On startup, opens a `ServiceHost` with `NetTcpBinding` at
`net.tcp://localhost:8090/BalanceService`. Console stays open (`Console.ReadLine()` or a proper
wait handle) until Ctrl+C.

**Implementation (`BalanceService : IBalanceService`):**
- Constructs a `GrpcChannel` via `GrpcChannel.ForAddress("https://localhost:<presentation-port>", new GrpcChannelOptions { HttpHandler = new WinHttpHandler() })`. `WinHttpHandler` (from the `Grpc.Net.Client.Web` / `System.Net.Http.WinHttpHandler` NuGet package) is required because .NET Framework's default `HttpClientHandler` doesn't support HTTP/2 trailers — this is the same class of constraint that already forces gRPC-Web on the Angular frontend, just solved differently on the .NET Framework side.
- Calls the existing `MovementService.GetBalance` RPC with the given `accountId`.
- Maps the gRPC response to `BalanceResponse`. Unknown account / RPC failure → thrown as a `FaultException<BalanceFault>` (a `[DataContract]` fault type carrying a message), never a raw exception — this is the WCF-side "one global error handler per transport" from CLAUDE.md's coding standards, implemented as an `IErrorHandler` on the service behavior that catches anything unhandled.

**Configuration:** `app.config` holds the NetTcp base address and the gRPC host address (so it's
adjustable without a rebuild), matching the existing pattern of environment-based backend URLs on
the frontend side.

**Testing:** one integration-style test — start the `ServiceHost` in-process on a random/loopback
port, call `GetBalance` through a real `ChannelFactory<IBalanceService>` against a seeded account
on the already-running Testcontainers/dev SQL Server (via the real gRPC host, or by starting the
gRPC host in-process too if that's cheaper), assert the returned balance matches
`BalanceReaderTests`' expectation. If standing up the full gRPC host in a test is impractical,
falls back to testing `IErrorHandler`/fault-mapping logic in isolation with a fake gRPC client —
decided during implementation, not blocking this spec.

**Run instructions:** added to root `README.md` — "after starting the gRPC host, in a second
terminal: `cd src/Sygnia.Backend/src/Sygnia.Wcf.Gateway && dotnet run` (or run the built .exe)".

## Component 2: Sygnia.WpfClient

**Location:** `src/Sygnia.WpfClient/` (sibling to `Sygnia.Backend`/`Sygnia.Frontend` at the repo's
`src/` root).

**Project:** .NET Framework 4.8, WPF Application — matches the gateway's era so the service
contract (`IBalanceService`) can be shared directly (project reference or linked file) without a
generated proxy, avoiding an `Add Service Reference` step.

**UI:** a single window —
- `TextBox` for account ID (placeholder text `ACC-001`)
- `Button` "Get Balance"
- `TextBlock`/`Label` showing `<currency> <balance>` on success, or the fault message on failure
- No navigation, no extra views — "does not have to be a complex app" per the brief note.

**Client code:** `ChannelFactory<IBalanceService>` over a `NetTcpBinding` pointed at
`net.tcp://localhost:8090/BalanceService` (same address as the gateway's `app.config`, kept in
sync manually since this is a demo utility, not a distributed config system). Click handler is
`async void` calling the WCF proxy, catching `FaultException<BalanceFault>` to show a friendly
error rather than crashing.

**Testing:** none — thin UI shell, verified manually by running it against the gateway.

## What's explicitly out of scope

- No retry/resilience policies on either the WCF→gRPC or WPF→WCF hop — single attempt, surfaced
  error on failure. Matches the "make reasonable simplifications, call them out in SOLUTION.md"
  guidance.
- No authentication on the NetTcp endpoint (matches the rest of the take-home's unauthenticated
  surface).
- No statement/transfer operations exposed through WCF or WPF — brief asks for exactly one
  operation (`GetBalance`) on the gateway, and the WPF client only needs to demonstrate that path.

## SOLUTION.md updates

Once built, `docs/SOLUTION.md`'s "Deliberate scope omissions" entry for the WCF gateway moves to
an "Implemented" section, documenting: the `WinHttpHandler` requirement for HTTP/2-over-.NET-
Framework, the fault-mapping choice, and the WPF client's role as the "legacy tool" consuming it.
