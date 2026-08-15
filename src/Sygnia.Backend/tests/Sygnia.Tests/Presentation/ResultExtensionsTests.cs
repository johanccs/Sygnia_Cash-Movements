using Grpc.Core;
using Sygnia.Domain;
using Sygnia.Presentation;

namespace Sygnia.Tests.Presentation;

public sealed class ResultExtensionsTests
{
    [Theory]
    [InlineData("account.not_found", StatusCode.NotFound)]
    [InlineData("movement.already_exists", StatusCode.AlreadyExists)]
    [InlineData("movement.invalid", StatusCode.InvalidArgument)]
    [InlineData("transfer.invalid", StatusCode.InvalidArgument)]
    [InlineData("balance.invalid", StatusCode.InvalidArgument)]
    [InlineData("statement.invalid", StatusCode.InvalidArgument)]
    [InlineData("movement.conflict_unresolved", StatusCode.Internal)]
    [InlineData("some.unmapped.code", StatusCode.Internal)]
    public void ToRpcException_MapsErrorCodeToStatus(string code, StatusCode expected)
    {
        var error = new Error(code, "message");

        var exception = error.ToRpcException();

        Assert.Equal(expected, exception.StatusCode);
        Assert.Equal("message", exception.Status.Detail);
    }
}
