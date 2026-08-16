using Grpc.Core;
using Sygnia.Domain;
using Sygnia.Presentation;

namespace Sygnia.Tests.Presentation;

public sealed class ResultExtensionsTests
{
    [Theory]
    [InlineData("account.not_found", StatusCode.NotFound)]
    [InlineData("movement.already_exists", StatusCode.AlreadyExists)]
    [InlineData("account.already_exists", StatusCode.AlreadyExists)]
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

    /// <summary>
    /// Pins every <see cref="ErrorCode"/> the codebase actually raises (grep for
    /// <c>new Error(</c> call sites) to its expected gRPC status, so a future addition to the
    /// enum that isn't wired into <c>ResultExtensions.ToRpcException</c> is caught here instead
    /// of silently falling through to <see cref="StatusCode.Internal"/>.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllKnownErrorCodes))]
    public void ToRpcException_MapsEveryKnownErrorCode(ErrorCode code, StatusCode expected)
    {
        var error = new Error(code, "message");

        var exception = error.ToRpcException();

        Assert.Equal(expected, exception.StatusCode);
    }

    public static IEnumerable<object[]> AllKnownErrorCodes() =>
        Enum.GetValues<ErrorCode>().Select(code => new object[] { code, ExpectedStatusFor(code) });

    private static StatusCode ExpectedStatusFor(ErrorCode code) => code switch
    {
        ErrorCode.AccountNotFound or ErrorCode.UserNotFound => StatusCode.NotFound,
        ErrorCode.AccountAlreadyExists or ErrorCode.MovementAlreadyExists or ErrorCode.UserAlreadyExists
            => StatusCode.AlreadyExists,
        ErrorCode.AccountInvalid or ErrorCode.AccountNameInvalid or ErrorCode.AccountContactPersonInvalid
            or ErrorCode.MovementInvalid or ErrorCode.MovementCurrencyInvalid or ErrorCode.TransferInvalid
            or ErrorCode.BalanceInvalid or ErrorCode.StatementInvalid or ErrorCode.UserInvalid
            => StatusCode.InvalidArgument,
        ErrorCode.MovementConflictUnresolved => StatusCode.Internal,
        _ => throw new NotSupportedException(
            $"{code} has no expected StatusCode in this test — add one before wiring it into ToRpcException."),
    };
}
