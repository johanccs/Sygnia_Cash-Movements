using Sygnia.Application.Commands.SubmitMovement;
using Sygnia.Tests.Application.Fakes;

namespace Sygnia.Tests.Application;

public sealed class SubmitMovementCommandHandlerTests
{
    private const string AccountId = "ACC-001";

    private static readonly DateTime OccurredAt = new(2024, 7, 15, 10, 42, 31, DateTimeKind.Utc);

    private static SubmitMovementCommand CreateCommand(string externalRef = "MOV-20240715-000123", decimal amount = 12500.00m) =>
        new(AccountId, externalRef, "ZAR", amount, OccurredAt, "Initial deposit", Guid.NewGuid(), "jsmith", OccurredAt);

    private static (SubmitMovementCommandHandler Handler, FakeMovementRepository Repository) CreateSut()
    {
        var repository = new FakeMovementRepository { ExistingAccountIds = { AccountId } };
        var handler = new SubmitMovementCommandHandler(repository, new SubmitMovementCommandValidator());
        return (handler, repository);
    }

    [Fact]
    public async Task Handle_UnknownAccount_ReturnsNotFoundFailure()
    {
        var repository = new FakeMovementRepository();
        var handler = new SubmitMovementCommandHandler(repository, new SubmitMovementCommandValidator());

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("account.not_found", result.Error.Code);
    }

    [Fact]
    public async Task Handle_InvalidCommand_ReturnsValidationFailure()
    {
        var (handler, _) = CreateSut();
        var command = CreateCommand(amount: 0m); // zero amount is invalid

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("movement.invalid", result.Error.Code);
    }

    [Fact]
    public async Task Handle_NewMovement_ReturnsSuccessAndStoresIt()
    {
        var (handler, _) = CreateSut();

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(AccountId, result.Value.AccountId);
    }

    [Fact]
    public async Task Handle_SameKeySameFields_IsIdempotentReplay()
    {
        var (handler, _) = CreateSut();
        var command = CreateCommand();

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value.RefNr, second.Value.RefNr);
    }

    [Fact]
    public async Task Handle_SameKeyDifferentFields_ReturnsAlreadyExists()
    {
        var (handler, _) = CreateSut();
        await handler.Handle(CreateCommand(), CancellationToken.None);

        var conflicting = await handler.Handle(CreateCommand(amount: 999.00m), CancellationToken.None);

        Assert.True(conflicting.IsFailure);
        Assert.Equal("movement.already_exists", conflicting.Error.Code);
    }
}
