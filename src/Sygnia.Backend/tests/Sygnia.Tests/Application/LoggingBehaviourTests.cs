using Microsoft.Extensions.Logging.Abstractions;
using Sygnia.Application.Behaviours;

namespace Sygnia.Tests.Application;

public sealed class LoggingBehaviourTests
{
    private sealed record Ping : MediatR.IRequest<string>;

    [Fact]
    public async Task Handle_DelegatesToNext_ReturnsItsResultUnchanged()
    {
        var behaviour = new LoggingBehaviour<Ping, string>(NullLogger<LoggingBehaviour<Ping, string>>.Instance);

        var result = await behaviour.Handle(new Ping(), () => Task.FromResult("pong"), CancellationToken.None);

        Assert.Equal("pong", result);
    }

    [Fact]
    public async Task Handle_NextThrows_PropagatesTheSameException()
    {
        var behaviour = new LoggingBehaviour<Ping, string>(NullLogger<LoggingBehaviour<Ping, string>>.Instance);
        var thrown = new InvalidOperationException("boom");

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => behaviour.Handle(new Ping(), () => throw thrown, CancellationToken.None));

        Assert.Same(thrown, actual);
    }
}
