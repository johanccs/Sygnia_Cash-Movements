using Grpc.Core;

namespace Sygnia.Tests.Presentation;

/// <summary>
/// Minimal <see cref="ServerCallContext"/> stand-in for unit-testing gRPC service
/// implementations without a live server. Only <see cref="CancellationToken"/> is exercised by
/// today's handlers; the rest of the abstract surface is filled in with harmless defaults.
/// </summary>
internal sealed class TestServerCallContext : ServerCallContext
{
    private TestServerCallContext(CancellationToken cancellationToken)
    {
        CancellationTokenCore = cancellationToken;
    }

    public static ServerCallContext Create(CancellationToken cancellationToken = default) =>
        new TestServerCallContext(cancellationToken);

    protected override string MethodCore { get; } = "TestMethod";

    protected override string HostCore { get; } = "localhost";

    protected override string PeerCore { get; } = "test-peer";

    protected override DateTime DeadlineCore { get; } = DateTime.UtcNow.AddMinutes(5);

    protected override Metadata RequestHeadersCore { get; } = new();

    protected override CancellationToken CancellationTokenCore { get; }

    protected override Metadata ResponseTrailersCore { get; } = new();

    protected override Status StatusCore { get; set; }

    protected override WriteOptions? WriteOptionsCore { get; set; }

    protected override AuthContext AuthContextCore { get; } = new AuthContext(null, new Dictionary<string, List<AuthProperty>>());

    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;

    protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) =>
        throw new NotSupportedException("Context propagation is not exercised by these tests.");
}
