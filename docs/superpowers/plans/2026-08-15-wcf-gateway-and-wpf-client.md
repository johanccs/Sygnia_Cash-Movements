# WCF Gateway + WPF Client Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the optional Task 3 legacy gateway (WCF NetTcp `GetBalance`, backed by the existing gRPC `GetBalance` RPC) and a minimal WPF client that consumes it, closing the two `MISSING (optional)` items from the brief-compliance audit.

**Architecture:** `Sygnia.WpfClient` (WPF, net48) → NetTcp `ChannelFactory<IBalanceService>` → `Sygnia.Wcf.Gateway` (self-hosted console, net48) → `Grpc.Net.Client` + `WinHttpHandler` → `Sygnia.Presentation` gRPC host (net8.0, unchanged) → existing `MovementService.GetBalance` RPC.

**Tech Stack:** .NET Framework 4.8, WCF (`System.ServiceModel`, framework reference — not a NuGet client-only package, since `ServiceHost` requires the full framework assembly), `Grpc.Net.Client` + `System.Net.Http.WinHttpHandler` for HTTP/2 gRPC from .NET Framework, WPF (`Microsoft.NET.Sdk.WindowsDesktop`), xUnit for the gateway's test.

**Spec:** `docs/superpowers/specs/2026-08-15-wcf-gateway-and-wpf-client-design.md`

## Global Constraints

- Both new projects target **net48**, overriding the `net8.0` default from `src/Sygnia.Backend/Directory.Build.props` — that override must happen inside each project's own `.csproj`, since `Directory.Build.props` is auto-imported and a later property assignment wins.
- Both projects also inherit `src/Sygnia.Backend/Directory.Packages.props` (central package management) — every NuGet package version goes there, never inline in a `.csproj`.
- `Sygnia.Wcf.Gateway` lives at `src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/` and is added to `Sygnia.Backend.sln` under the existing `src` solution folder (matches the CLAUDE.md architecture diagram).
- `Sygnia.WpfClient` lives at `src/Sygnia.WpfClient/`, sibling to `Sygnia.Backend`/`Sygnia.Frontend` — **not** added to `Sygnia.Backend.sln` (same relationship `Sygnia.Frontend` has to that solution: independent, not part of it).
- Amount/balance stays a decimal-safe string end-to-end (matches `GetBalanceResponse.balance` on the wire) — never parsed to `double`.
- Faults on the WCF side are always `FaultException<BalanceFault>`, never a raw/unhandled exception — CLAUDE.md's "one global error handler per transport" rule, implemented via `IErrorHandler`.
- gRPC host's Kestrel is already `Http1AndHttp2` (`src/Sygnia.Backend/src/Sygnia.Presentation/appsettings.json`) and dev address is `https://localhost:7110` (`launchSettings.json`) — no changes needed there.

---

### Task 1: Scaffold `Sygnia.Wcf.Gateway` project and register it in the solution

**Files:**
- Create: `src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/Sygnia.Wcf.Gateway.csproj`
- Create: `src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/Program.cs` (placeholder `Console.WriteLine` for now — Task 4 replaces it)
- Modify: `src/Sygnia.Backend/Sygnia.Backend.sln`
- Modify: `src/Sygnia.Backend/Directory.Packages.props`

**Interfaces:**
- Produces: a buildable console project named `Sygnia.Wcf.Gateway`, referenced by no other project, that Tasks 2-6 build on.

- [ ] **Step 1: Add the new package versions to central package management**

Add to `src/Sygnia.Backend/Directory.Packages.props`, inside the first `<ItemGroup>` (after the `OpenTelemetry.Instrumentation.AspNetCore` line):

```xml
    <PackageVersion Include="Grpc.Net.Client" Version="2.57.0" />
    <PackageVersion Include="Google.Protobuf" Version="3.25.1" />
    <PackageVersion Include="Grpc.Tools" Version="2.57.0" />
    <PackageVersion Include="System.Net.Http.WinHttpHandler" Version="8.0.0" />
```

- [ ] **Step 2: Create the project file**

Create `src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/Sygnia.Wcf.Gateway.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <!--
    .NET Framework 4.8, deliberately outside the net8.0 default in Directory.Build.props:
    ServiceHost (WCF server hosting) requires the full framework, and this is the one
    project in the solution that can't share that default. See CLAUDE.md's Known traps.
  -->
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net48</TargetFramework>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>false</EnforceCodeStyleInBuild>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include="System.ServiceModel" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Grpc.Net.Client" />
    <PackageReference Include="Google.Protobuf" />
    <PackageReference Include="Grpc.Tools" PrivateAssets="All" />
    <PackageReference Include="System.Net.Http.WinHttpHandler" />
  </ItemGroup>

  <ItemGroup>
    <Protobuf Include="..\Sygnia.Presentation\Protos\movements.proto" GrpcServices="Client" Link="Protos\movements.proto" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Create a placeholder entry point**

Create `src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/Program.cs`:

```csharp
using System;

namespace Sygnia.Wcf.Gateway
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Sygnia.Wcf.Gateway placeholder — replaced in Task 4.");
        }
    }
}
```

- [ ] **Step 4: Verify it builds standalone**

Run: `dotnet build src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/Sygnia.Wcf.Gateway.csproj`
Expected: build succeeds, and the generated gRPC client stubs appear under `obj/.../Protos/Movements/` (proof the `Protobuf` client-codegen reference works from a net48 project).

- [ ] **Step 5: Add the project to the solution**

Run:
```bash
dotnet sln src/Sygnia.Backend/Sygnia.Backend.sln add src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/Sygnia.Wcf.Gateway.csproj --solution-folder src
```

- [ ] **Step 6: Verify the whole solution still builds**

Run: `dotnet build src/Sygnia.Backend/Sygnia.Backend.sln`
Expected: all projects, including the new one, build with no errors.

- [ ] **Step 7: Commit**

```bash
git add src/Sygnia.Backend/src/Sygnia.Wcf.Gateway src/Sygnia.Backend/Sygnia.Backend.sln src/Sygnia.Backend/Directory.Packages.props
git commit -m "Scaffold Sygnia.Wcf.Gateway project targeting net48"
```

---

### Task 2: Define the WCF contract (`IBalanceService`, `BalanceResponse`, `BalanceFault`)

**Files:**
- Create: `src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/Contracts/IBalanceService.cs`
- Create: `src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/Contracts/BalanceResponse.cs`
- Create: `src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/Contracts/BalanceFault.cs`

**Interfaces:**
- Produces: `IBalanceService.GetBalance(string accountId) : BalanceResponse`, `BalanceResponse { AccountId, Balance }`, `BalanceFault { Message }` — consumed by Task 3 (implementation), Task 4 (hosting), Task 5 (test), and later by `Sygnia.WpfClient` (Task 8, via a linked copy of this same file — see Task 7).

- [ ] **Step 1: Write the service contract**

Create `src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/Contracts/IBalanceService.cs`:

```csharp
using System.ServiceModel;

namespace Sygnia.Wcf.Gateway.Contracts
{
    [ServiceContract(Namespace = "http://sygnia.local/balance")]
    public interface IBalanceService
    {
        [OperationContract]
        [FaultContract(typeof(BalanceFault))]
        BalanceResponse GetBalance(string accountId);
    }
}
```

- [ ] **Step 2: Write the response data contract**

Create `src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/Contracts/BalanceResponse.cs`:

```csharp
using System.Runtime.Serialization;

namespace Sygnia.Wcf.Gateway.Contracts
{
    [DataContract(Namespace = "http://sygnia.local/balance")]
    public class BalanceResponse
    {
        [DataMember]
        public string AccountId { get; set; }

        // Decimal-as-string, matching the gRPC wire format (GetBalanceResponse.balance) —
        // never a double, which corrupts cents.
        [DataMember]
        public string Balance { get; set; }
    }
}
```

- [ ] **Step 3: Write the fault data contract**

Create `src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/Contracts/BalanceFault.cs`:

```csharp
using System.Runtime.Serialization;

namespace Sygnia.Wcf.Gateway.Contracts
{
    [DataContract(Namespace = "http://sygnia.local/balance")]
    public class BalanceFault
    {
        [DataMember]
        public string Message { get; set; }
    }
}
```

- [ ] **Step 4: Verify it builds**

Run: `dotnet build src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/Sygnia.Wcf.Gateway.csproj`
Expected: build succeeds (no test yet — contracts have no behavior to test on their own).

- [ ] **Step 5: Commit**

```bash
git add src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/Contracts
git commit -m "Add WCF balance service contract"
```

---

### Task 3: Implement `BalanceService` (calls the gRPC `GetBalance` RPC) with fault mapping

**Files:**
- Create: `src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/BalanceService.cs`
- Create: `src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/GrpcErrorHandler.cs`
- Test: `src/Sygnia.Backend/tests/Sygnia.Tests/Wcf/BalanceServiceTests.cs`

**Interfaces:**
- Consumes: `IBalanceService`, `BalanceResponse`, `BalanceFault` (Task 2); generated gRPC client `Movements.MovementService.MovementServiceClient` from the `movements.proto` client codegen (Task 1); `Grpc.Net.Client.GrpcChannel`.
- Produces: `BalanceService : IBalanceService`, constructed as `new BalanceService(Movements.MovementService.MovementServiceClient client)` — consumed by Task 4 (hosting) and Task 5 (test drives it directly).
- Produces: `GrpcErrorHandler : IErrorHandler` — consumed by Task 4 (attached to the `ServiceHost`).

- [ ] **Step 1: Write the failing test for a successful balance lookup**

Create `src/Sygnia.Backend/tests/Sygnia.Tests/Wcf/BalanceServiceTests.cs`. This test won't run on net8.0 test project directly against a net48 `BalanceService` type — instead it verifies the *mapping logic* by driving `BalanceService` against a fake gRPC client interface. Since the generated gRPC client is a concrete `sealed`-by-generation class that's hard to fake directly, `BalanceService` takes a small delegate-shaped seam instead of the raw generated client, keeping the test in the existing `Sygnia.Tests` project simple:

```csharp
using System;
using Grpc.Core;
using Sygnia.Wcf.Gateway;
using Sygnia.Wcf.Gateway.Contracts;
using Xunit;

namespace Sygnia.Tests.Wcf;

public class BalanceServiceTests
{
    [Fact]
    public void GetBalance_KnownAccount_ReturnsMappedBalance()
    {
        var service = new BalanceService(accountId =>
        {
            Assert.Equal("ACC-001", accountId);
            return ("ACC-001", "750.0000");
        });

        var result = service.GetBalance("ACC-001");

        Assert.Equal("ACC-001", result.AccountId);
        Assert.Equal("750.0000", result.Balance);
    }

    [Fact]
    public void GetBalance_RpcThrowsNotFound_ThrowsBalanceFaultWithMessage()
    {
        var service = new BalanceService(accountId =>
            throw new RpcException(new Status(StatusCode.NotFound, "Unknown account 'ACC-999'.")));

        var ex = Assert.Throws<FaultException<BalanceFault>>(() => service.GetBalance("ACC-999"));

        Assert.Equal("Unknown account 'ACC-999'.", ex.Detail.Message);
    }
}
```

Note: this test file lives in `Sygnia.Tests` (net8.0), so it can only compile once `Sygnia.Wcf.Gateway`'s public types are referenceable from a net8.0 project. `BalanceService` and its contracts have no net48-only API surface (no `ServiceHost`, no framework-only types), so add a `<ProjectReference Include="..\..\src\Sygnia.Wcf.Gateway\Sygnia.Wcf.Gateway.csproj" />` to `Sygnia.Tests.csproj` — cross-targeting a net48 library from a net8.0 test project is supported by the SDK as long as the net48 project has no framework-only APIs in its public surface, which holds here (WCF hosting stays in `Program.cs`, untested).

- [ ] **Step 2: Add the test project reference**

Modify `src/Sygnia.Backend/tests/Sygnia.Tests/Sygnia.Tests.csproj` — add inside the existing `<ItemGroup>` of `ProjectReference`s:

```xml
    <ProjectReference Include="..\..\src\Sygnia.Wcf.Gateway\Sygnia.Wcf.Gateway.csproj" />
```

- [ ] **Step 3: Run the test to verify it fails**

Run: `dotnet test src/Sygnia.Backend/tests/Sygnia.Tests --filter FullyQualifiedName~BalanceServiceTests`
Expected: build error — `BalanceService` doesn't exist yet.

- [ ] **Step 4: Implement `BalanceService`**

Create `src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/BalanceService.cs`:

```csharp
using System;
using System.ServiceModel;
using Grpc.Core;
using Sygnia.Wcf.Gateway.Contracts;

namespace Sygnia.Wcf.Gateway
{
    // The gRPC call is expressed as a delegate rather than the concrete generated client so
    // this class stays trivially testable from the net8.0 Sygnia.Tests project without a real
    // channel: Func<accountId, (accountId, balance)>.
    public class BalanceService : IBalanceService
    {
        private readonly Func<string, (string AccountId, string Balance)> getBalance;

        public BalanceService(Func<string, (string AccountId, string Balance)> getBalance)
        {
            this.getBalance = getBalance ?? throw new ArgumentNullException(nameof(getBalance));
        }

        public BalanceResponse GetBalance(string accountId)
        {
            try
            {
                var (id, balance) = getBalance(accountId);
                return new BalanceResponse { AccountId = id, Balance = balance };
            }
            catch (RpcException ex)
            {
                throw new FaultException<BalanceFault>(
                    new BalanceFault { Message = ex.Status.Detail },
                    new FaultReason(ex.Status.Detail));
            }
        }
    }
}
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test src/Sygnia.Backend/tests/Sygnia.Tests --filter FullyQualifiedName~BalanceServiceTests`
Expected: both tests PASS.

- [ ] **Step 6: Write `GrpcErrorHandler` (catches anything unhandled at the WCF boundary)**

Create `src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/GrpcErrorHandler.cs`:

```csharp
using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using Sygnia.Wcf.Gateway.Contracts;

namespace Sygnia.Wcf.Gateway
{
    // One global error handler per transport (CLAUDE.md coding standards): anything that
    // escapes BalanceService.GetBalance uncaught is logged and turned into a clean
    // FaultException<BalanceFault> here, never a raw exception on the wire.
    public class GrpcErrorHandler : IErrorHandler
    {
        public bool HandleError(Exception error)
        {
            Console.Error.WriteLine($"[Sygnia.Wcf.Gateway] Unhandled error: {error}");
            return true;
        }

        public void ProvideFault(Exception error, MessageVersion version, ref System.ServiceModel.Channels.Message fault)
        {
            var faultException = new System.ServiceModel.FaultException<BalanceFault>(
                new BalanceFault { Message = "An unexpected error occurred." },
                new System.ServiceModel.FaultReason("An unexpected error occurred."));

            var faultMessageFault = faultException.CreateMessageFault();
            fault = System.ServiceModel.Channels.Message.CreateMessage(version, faultMessageFault, faultException.Action);
        }
    }
}
```

- [ ] **Step 7: Run the full test project to confirm nothing else broke**

Run: `dotnet test src/Sygnia.Backend/tests/Sygnia.Tests`
Expected: all tests PASS, including the two new ones.

- [ ] **Step 8: Commit**

```bash
git add src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/BalanceService.cs src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/GrpcErrorHandler.cs src/Sygnia.Backend/tests/Sygnia.Tests/Wcf src/Sygnia.Backend/tests/Sygnia.Tests/Sygnia.Tests.csproj
git commit -m "Implement BalanceService with gRPC-to-WCF fault mapping"
```

---

### Task 4: Host the WCF service and wire it to a real gRPC channel

**Files:**
- Modify: `src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/Program.cs`
- Create: `src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/App.config`

**Interfaces:**
- Consumes: `BalanceService` (Task 3), `GrpcErrorHandler` (Task 3), `Movements.MovementService.MovementServiceClient` (generated in Task 1).
- Produces: a running `net.tcp://localhost:8090/BalanceService` endpoint — consumed by Task 6 (README instructions) and Task 8 (WPF client).

- [ ] **Step 1: Write the configuration file**

Create `src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/App.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <appSettings>
    <add key="GrpcHostAddress" value="https://localhost:7110" />
    <add key="NetTcpBaseAddress" value="net.tcp://localhost:8090/BalanceService" />
  </appSettings>
  <startup>
    <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8" />
  </startup>
</configuration>
```

- [ ] **Step 2: Implement `Program.cs`**

Replace `src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/Program.cs`:

```csharp
using System;
using System.Configuration;
using System.Net.Http;
using System.ServiceModel;
using System.ServiceModel.Description;
using Grpc.Net.Client;
using Movements;
using Sygnia.Wcf.Gateway.Contracts;

namespace Sygnia.Wcf.Gateway
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var grpcHostAddress = ConfigurationManager.AppSettings["GrpcHostAddress"] ?? "https://localhost:7110";
            var netTcpBaseAddress = ConfigurationManager.AppSettings["NetTcpBaseAddress"] ?? "net.tcp://localhost:8090/BalanceService";

            // .NET Framework's default HttpClientHandler doesn't support HTTP/2 trailers,
            // so gRPC needs WinHttpHandler here — the same class of constraint that forces
            // gRPC-Web on the Angular frontend, solved differently on this side.
            var httpHandler = new WinHttpHandler();
            var channel = GrpcChannel.ForAddress(grpcHostAddress, new GrpcChannelOptions { HttpHandler = httpHandler });
            var grpcClient = new MovementService.MovementServiceClient(channel);

            var balanceService = new BalanceService(accountId =>
            {
                var response = grpcClient.GetBalance(new GetBalanceRequest { AccountId = accountId });
                return (response.AccountId, response.Balance);
            });

            using (var host = new ServiceHost(balanceService))
            {
                host.AddServiceEndpoint(typeof(IBalanceService), new NetTcpBinding(SecurityMode.None), netTcpBaseAddress);

                foreach (var behavior in host.Description.Behaviors)
                {
                    if (behavior is ServiceDebugBehavior debug)
                    {
                        debug.IncludeExceptionDetailInFaults = false;
                    }
                }

                foreach (var endpointDispatcher in host.ChannelDispatchers)
                {
                    // ChannelDispatcher exposes ErrorHandlers only after Open(); attach via
                    // a behavior instead so it's present before the host opens.
                }

                host.Description.Behaviors.Add(new ErrorHandlerBehavior());
                host.Open();

                Console.WriteLine($"Sygnia.Wcf.Gateway listening on {netTcpBaseAddress}");
                Console.WriteLine($"Forwarding to gRPC host at {grpcHostAddress}");
                Console.WriteLine("Press Enter to stop.");
                Console.ReadLine();

                host.Close();
            }
        }
    }
}
```

- [ ] **Step 3: Write the behavior that attaches `GrpcErrorHandler` to every endpoint dispatcher**

Create `src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/ErrorHandlerBehavior.cs`:

```csharp
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Sygnia.Wcf.Gateway
{
    public class ErrorHandlerBehavior : IServiceBehavior
    {
        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase,
            Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            foreach (var channelDispatcherBase in serviceHostBase.ChannelDispatchers)
            {
                if (channelDispatcherBase is ChannelDispatcher channelDispatcher)
                {
                    channelDispatcher.ErrorHandlers.Add(new GrpcErrorHandler());
                }
            }
        }

        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
        }
    }
}
```

- [ ] **Step 4: Remove the dead loop from `Program.cs`**

Modify `src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/Program.cs` — delete the empty `foreach (var endpointDispatcher in host.ChannelDispatchers) { }` block added in Step 2 (the behavior in Step 3 replaces it):

```csharp
                foreach (var behavior in host.Description.Behaviors)
                {
                    if (behavior is ServiceDebugBehavior debug)
                    {
                        debug.IncludeExceptionDetailInFaults = false;
                    }
                }

                host.Description.Behaviors.Add(new ErrorHandlerBehavior());
```

(This replaces the two `foreach` blocks that followed `host.AddServiceEndpoint` with just these two statements.)

- [ ] **Step 5: Build and manually smoke-test**

Run: `dotnet build src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/Sygnia.Wcf.Gateway.csproj`
Expected: build succeeds.

In one terminal: `dotnet run --project src/Sygnia.Backend/src/Sygnia.Presentation` (starts the gRPC host).
In a second terminal: `dotnet run --project src/Sygnia.Backend/src/Sygnia.Wcf.Gateway` (starts the gateway).
Expected console output: `Sygnia.Wcf.Gateway listening on net.tcp://localhost:8090/BalanceService`. Leave both running for Task 5's test and Task 8's manual client check; stop with Enter/Ctrl+C when done.

- [ ] **Step 6: Commit**

```bash
git add src/Sygnia.Backend/src/Sygnia.Wcf.Gateway
git commit -m "Host the WCF NetTcp GetBalance endpoint over a real gRPC channel"
```

---

### Task 5: Integration test — real `ServiceHost` + real `ChannelFactory` round trip

**Files:**
- Test: `src/Sygnia.Backend/tests/Sygnia.Tests/Wcf/BalanceServiceHostingTests.cs`

This test lives in the net8.0 `Sygnia.Tests` project and exercises `BalanceService` + `GrpcErrorHandler` + `ErrorHandlerBehavior` hosted in a real in-process `ServiceHost`, called through a real `ChannelFactory<IBalanceService>` — proving the WCF plumbing itself (not just `BalanceService`'s mapping logic, already covered in Task 3) works end to end. It does not start the real gRPC host; the `BalanceService` delegate is a fake, matching the seam from Task 3.

**Interfaces:**
- Consumes: `BalanceService`, `IBalanceService`, `BalanceFault`, `ErrorHandlerBehavior` (Task 3-4).

- [ ] **Step 1: Write the failing test**

Create `src/Sygnia.Backend/tests/Sygnia.Tests/Wcf/BalanceServiceHostingTests.cs`:

```csharp
using System;
using System.ServiceModel;
using Grpc.Core;
using Sygnia.Wcf.Gateway;
using Sygnia.Wcf.Gateway.Contracts;
using Xunit;

namespace Sygnia.Tests.Wcf;

public class BalanceServiceHostingTests : IDisposable
{
    private readonly ServiceHost host;
    private readonly string address;

    public BalanceServiceHostingTests()
    {
        address = $"net.tcp://localhost:{GetFreePort()}/BalanceService";

        var service = new BalanceService(accountId => accountId == "ACC-001"
            ? ("ACC-001", "750.0000")
            : throw new RpcException(new Status(StatusCode.NotFound, $"Unknown account '{accountId}'.")));

        host = new ServiceHost(service);
        host.AddServiceEndpoint(typeof(IBalanceService), new NetTcpBinding(SecurityMode.None), address);
        host.Description.Behaviors.Add(new ErrorHandlerBehavior());
        host.Open();
    }

    [Fact]
    public void GetBalance_KnownAccount_ReturnsBalanceOverRealChannel()
    {
        var factory = new ChannelFactory<IBalanceService>(new NetTcpBinding(SecurityMode.None), new EndpointAddress(address));
        var proxy = factory.CreateChannel();

        var result = proxy.GetBalance("ACC-001");

        Assert.Equal("ACC-001", result.AccountId);
        Assert.Equal("750.0000", result.Balance);
    }

    [Fact]
    public void GetBalance_UnknownAccount_ThrowsBalanceFaultOverRealChannel()
    {
        var factory = new ChannelFactory<IBalanceService>(new NetTcpBinding(SecurityMode.None), new EndpointAddress(address));
        var proxy = factory.CreateChannel();

        var ex = Assert.Throws<FaultException<BalanceFault>>(() => proxy.GetBalance("ACC-999"));

        Assert.Equal("Unknown account 'ACC-999'.", ex.Detail.Message);
    }

    public void Dispose()
    {
        host.Close();
    }

    private static int GetFreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
```

- [ ] **Step 2: Run the test to verify it fails for the right reason**

Run: `dotnet test src/Sygnia.Backend/tests/Sygnia.Tests --filter FullyQualifiedName~BalanceServiceHostingTests`
Expected: FAIL — `System.ServiceModel` types (`ServiceHost`, `ChannelFactory`, `NetTcpBinding`) aren't available in the net8.0 test project by default.

- [ ] **Step 3: Add the WCF client package to the test project**

Add to `src/Sygnia.Backend/Directory.Packages.props`, same `<ItemGroup>` as Step 1 of Task 1:

```xml
    <PackageVersion Include="System.ServiceModel.NetTcp" Version="6.2.0" />
```

Modify `src/Sygnia.Backend/tests/Sygnia.Tests/Sygnia.Tests.csproj` — add to its `PackageReference` `<ItemGroup>`:

```xml
    <PackageReference Include="System.ServiceModel.NetTcp" />
```

This CoreWCF-compatible client package provides `ChannelFactory`/`NetTcpBinding` on net8.0 for the *client* side of the test; `ServiceHost` (server-side hosting) is only available via the net48 `Sygnia.Wcf.Gateway` project reference already added in Task 3 — the test's `new ServiceHost(...)` call resolves from there because the test project multi-targets by referencing a net48 library, and `ServiceHost` ships in the net48 reference assembly `System.ServiceModel` that `Sygnia.Wcf.Gateway.csproj` already references. If `ServiceHost` is not visible to the net8.0 test project (framework reference assemblies don't flow across a cross-targeting `ProjectReference`), fall back immediately: move `BalanceServiceHostingTests` into a **new net48 xUnit test project** `src/Sygnia.Backend/tests/Sygnia.Wcf.Gateway.Tests/` referencing `Sygnia.Wcf.Gateway.csproj` directly, add it to `Sygnia.Backend.sln` under `tests`, and re-run from there instead.

- [ ] **Step 4: Run the test again**

Run: `dotnet test src/Sygnia.Backend/tests/Sygnia.Tests --filter FullyQualifiedName~BalanceServiceHostingTests`
Expected: PASS if `ServiceHost` resolved from the cross-targeted reference; otherwise apply the Step 3 fallback (new net48 test project) and re-run from there — Expected: PASS.

- [ ] **Step 5: Run the whole test suite**

Run: `dotnet test src/Sygnia.Backend/tests/Sygnia.Tests` (and the fallback project too, if created)
Expected: all tests PASS, no regressions.

- [ ] **Step 6: Commit**

```bash
git add src/Sygnia.Backend/tests src/Sygnia.Backend/Directory.Packages.props src/Sygnia.Backend/Sygnia.Backend.sln
git commit -m "Add real ServiceHost/ChannelFactory round-trip test for the WCF gateway"
```

---

### Task 6: README run instructions for the WCF gateway

**Files:**
- Modify: `README.md` (repo root)

**Interfaces:**
- Consumes: nothing code-level — documents Task 4's running gateway.

- [ ] **Step 1: Add a new section after the existing "Run the gRPC host" instructions**

Modify `README.md` — insert a new `### Run the WCF gateway (optional, Task 3)` section immediately after the gRPC host run instructions, with this content:

```markdown
### Run the WCF gateway (optional, Task 3)

`Sygnia.Wcf.Gateway` is a minimal legacy NetTcp gateway exposing one `GetBalance` operation. It
calls into the same backend as the gRPC API — by acting as a gRPC client itself — so both entry
points return identical balances for identical data.

With the gRPC host already running (see above), in a second terminal:

```bash
dotnet run --project src/Sygnia.Backend/src/Sygnia.Wcf.Gateway
```

It listens on `net.tcp://localhost:8090/BalanceService`. Both addresses (the gRPC host it calls,
and the NetTcp address it listens on) are configurable in
`src/Sygnia.Backend/src/Sygnia.Wcf.Gateway/App.config`.
```

- [ ] **Step 2: Commit**

```bash
git add README.md
git commit -m "Document how to run the WCF gateway"
```

---

### Task 7: Scaffold `Sygnia.WpfClient` and share the WCF contract

**Files:**
- Create: `src/Sygnia.WpfClient/Sygnia.WpfClient.csproj`
- Create: `src/Sygnia.WpfClient/App.xaml`
- Create: `src/Sygnia.WpfClient/App.xaml.cs`
- Create: `src/Sygnia.WpfClient/App.config`

**Interfaces:**
- Consumes: `IBalanceService`, `BalanceResponse`, `BalanceFault` from `Sygnia.Wcf.Gateway.Contracts` (Task 2), via a linked reference to those three files — no NuGet/service-reference step needed since both projects are net48.
- Produces: a buildable, empty WPF shell — Task 8 adds the window.

- [ ] **Step 1: Create the project file**

Create `src/Sygnia.WpfClient/Sygnia.WpfClient.csproj`. Note this project is **not** under `src/Sygnia.Backend/`, so it does not inherit that solution's `Directory.Build.props`/`Directory.Packages.props` — all properties and package versions are declared inline here, same as `Sygnia.Frontend` being independent of the backend build:

```xml
<Project Sdk="Microsoft.NET.Sdk.WindowsDesktop">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net48</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>disable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include="System.ServiceModel" />
  </ItemGroup>

  <!--
    Shares the WCF contract with Sygnia.Wcf.Gateway by linking the same files rather than
    duplicating them or generating a service reference — both projects are net48, so the
    types are usable as-is.
  -->
  <ItemGroup>
    <Compile Include="..\Sygnia.Backend\src\Sygnia.Wcf.Gateway\Contracts\IBalanceService.cs" Link="Contracts\IBalanceService.cs" />
    <Compile Include="..\Sygnia.Backend\src\Sygnia.Wcf.Gateway\Contracts\BalanceResponse.cs" Link="Contracts\BalanceResponse.cs" />
    <Compile Include="..\Sygnia.Backend\src\Sygnia.Wcf.Gateway\Contracts\BalanceFault.cs" Link="Contracts\BalanceFault.cs" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Create the app config (NetTcp address)**

Create `src/Sygnia.WpfClient/App.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <appSettings>
    <add key="GatewayAddress" value="net.tcp://localhost:8090/BalanceService" />
  </appSettings>
  <startup>
    <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8" />
  </startup>
</configuration>
```

- [ ] **Step 3: Create the application entry point**

Create `src/Sygnia.WpfClient/App.xaml`:

```xml
<Application x:Class="Sygnia.WpfClient.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             StartupUri="MainWindow.xaml">
</Application>
```

Create `src/Sygnia.WpfClient/App.xaml.cs`:

```csharp
using System.Windows;

namespace Sygnia.WpfClient
{
    public partial class App : Application
    {
    }
}
```

- [ ] **Step 4: Add a minimal placeholder window so the project builds**

Create `src/Sygnia.WpfClient/MainWindow.xaml`:

```xml
<Window x:Class="Sygnia.WpfClient.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Sygnia Balance Lookup" Height="180" Width="360">
    <Grid />
</Window>
```

Create `src/Sygnia.WpfClient/MainWindow.xaml.cs`:

```csharp
using System.Windows;

namespace Sygnia.WpfClient
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}
```

- [ ] **Step 5: Verify it builds**

Run: `dotnet build src/Sygnia.WpfClient/Sygnia.WpfClient.csproj`
Expected: build succeeds.

- [ ] **Step 6: Commit**

```bash
git add src/Sygnia.WpfClient
git commit -m "Scaffold Sygnia.WpfClient sharing the WCF balance contract"
```

---

### Task 8: Build the balance-lookup UI

**Files:**
- Modify: `src/Sygnia.WpfClient/MainWindow.xaml`
- Modify: `src/Sygnia.WpfClient/MainWindow.xaml.cs`

**Interfaces:**
- Consumes: `IBalanceService`, `BalanceResponse`, `BalanceFault` (Task 7's linked contract files).

- [ ] **Step 1: Build the window layout**

Modify `src/Sygnia.WpfClient/MainWindow.xaml`:

```xml
<Window x:Class="Sygnia.WpfClient.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Sygnia Balance Lookup" Height="180" Width="360">
    <StackPanel Margin="16">
        <TextBlock Text="Account ID" Margin="0,0,0,4" />
        <TextBox x:Name="AccountIdTextBox" Text="ACC-001" Margin="0,0,0,12" />
        <Button x:Name="GetBalanceButton" Content="Get Balance" Click="GetBalanceButton_Click" Width="120" HorizontalAlignment="Left" />
        <TextBlock x:Name="ResultTextBlock" Margin="0,16,0,0" TextWrapping="Wrap" />
    </StackPanel>
</Window>
```

- [ ] **Step 2: Implement the click handler**

Modify `src/Sygnia.WpfClient/MainWindow.xaml.cs`:

```csharp
using System;
using System.Configuration;
using System.ServiceModel;
using System.Windows;
using System.Windows.Media;
using Sygnia.Wcf.Gateway.Contracts;

namespace Sygnia.WpfClient
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void GetBalanceButton_Click(object sender, RoutedEventArgs e)
        {
            var accountId = AccountIdTextBox.Text.Trim();
            if (string.IsNullOrEmpty(accountId))
            {
                ShowResult("Enter an account ID.", isError: true);
                return;
            }

            var gatewayAddress = ConfigurationManager.AppSettings["GatewayAddress"] ?? "net.tcp://localhost:8090/BalanceService";
            var factory = new ChannelFactory<IBalanceService>(
                new NetTcpBinding(SecurityMode.None),
                new EndpointAddress(gatewayAddress));

            try
            {
                var proxy = factory.CreateChannel();
                var result = proxy.GetBalance(accountId);
                ShowResult($"{result.AccountId}: {result.Balance}", isError: false);
            }
            catch (FaultException<BalanceFault> fault)
            {
                ShowResult(fault.Detail.Message, isError: true);
            }
            catch (CommunicationException ex)
            {
                ShowResult($"Could not reach the gateway: {ex.Message}", isError: true);
            }
            finally
            {
                factory.Abort();
            }
        }

        private void ShowResult(string text, bool isError)
        {
            ResultTextBlock.Text = text;
            ResultTextBlock.Foreground = isError ? Brushes.Red : Brushes.Black;
        }
    }
}
```

- [ ] **Step 3: Build**

Run: `dotnet build src/Sygnia.WpfClient/Sygnia.WpfClient.csproj`
Expected: build succeeds.

- [ ] **Step 4: Manual verification**

With the gRPC host and WCF gateway both running (Task 4, Step 5), run:
```bash
dotnet run --project src/Sygnia.WpfClient
```
In the window: enter a seeded account ID (e.g. `ACC-001`, from `src/Sygnia.Backend/scripts/`), click "Get Balance", confirm the balance shown matches what `grpcurl`/the Angular UI shows for the same account (per README's existing gRPC verification instructions). Then try an unknown account ID and confirm the red error text shows the "Unknown account" message instead of a crash.

- [ ] **Step 5: Commit**

```bash
git add src/Sygnia.WpfClient/MainWindow.xaml src/Sygnia.WpfClient/MainWindow.xaml.cs
git commit -m "Add balance-lookup UI to Sygnia.WpfClient"
```

---

### Task 9: README run instructions for the WPF client

**Files:**
- Modify: `README.md` (repo root)

- [ ] **Step 1: Add a new section after the WCF gateway section**

Modify `README.md` — insert after the section added in Task 6:

```markdown
### Run the WPF client (optional)

`Sygnia.WpfClient` is a minimal desktop app that queries the WCF gateway's `GetBalance`
operation — the "legacy tool" side of the demo. Windows only.

With the gRPC host and the WCF gateway both running (see above), in a third terminal:

```bash
dotnet run --project src/Sygnia.WpfClient
```

Enter an account ID (e.g. `ACC-001`) and click **Get Balance**. The gateway address is
configurable in `src/Sygnia.WpfClient/App.config`.
```

- [ ] **Step 2: Commit**

```bash
git add README.md
git commit -m "Document how to run the WPF client"
```

---

### Task 10: Update SOLUTION.md

**Files:**
- Modify: `docs/SOLUTION.md`

- [ ] **Step 1: Move the WCF gateway from "omitted" to "implemented"**

Modify `docs/SOLUTION.md` — find the existing "Deliberate scope omissions" entry for the WCF gateway (per the compliance audit, it currently reads something like *"WCF (NetTcp) legacy gateway (Task 3, optional) — not implemented..."*) and replace that bullet with a removal, then add a new section documenting what was built:

```markdown
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
matching the rest of the take-home's unauthenticated surface.
```

- [ ] **Step 2: Commit**

```bash
git add docs/SOLUTION.md
git commit -m "Document the WCF gateway and WPF client in SOLUTION.md"
```

---

## Final verification

- [ ] Run: `dotnet build src/Sygnia.Backend/Sygnia.Backend.sln` — Expected: succeeds, including `Sygnia.Wcf.Gateway`.
- [ ] Run: `dotnet build src/Sygnia.WpfClient/Sygnia.WpfClient.csproj` — Expected: succeeds.
- [ ] Run: `dotnet test src/Sygnia.Backend/tests/Sygnia.Tests` — Expected: all tests pass, no regressions in the pre-existing suite.
- [ ] Manually run all three processes together (gRPC host, WCF gateway, WPF client) per Task 8 Step 4 and confirm a full round trip.
- [ ] Open a PR per CLAUDE.md's workflow — do not merge without approval.
