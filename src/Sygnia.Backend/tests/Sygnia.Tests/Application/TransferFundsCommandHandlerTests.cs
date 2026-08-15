using Sygnia.Application.Commands.TransferFunds;
using Sygnia.Tests.Application.Fakes;

namespace Sygnia.Tests.Application;

public sealed class TransferFundsCommandHandlerTests
{
    private static readonly DateTime OccurredAt = new(2024, 7, 15, 10, 42, 31, DateTimeKind.Utc);

    private static TransferFundsCommand CreateCommand() => new(
        "ACC-001",
        "ACC-002",
        "MOV-20240715-01",
        "ZAR",
        500.00m,
        OccurredAt,
        "Transfer",
        Guid.NewGuid(),
        "jsmith",
        OccurredAt);

    private static FakeAccountRepository CreateAccounts(params string[] accountIds)
    {
        var accounts = new FakeAccountRepository();
        foreach (var accountId in accountIds)
        {
            accounts.AddExisting(accountId);
        }

        return accounts;
    }

    [Fact]
    public async Task Handle_ValidTransfer_WritesOppositeSignedLegs()
    {
        var repository = new FakeMovementRepository();
        var handler = new TransferFundsCommandHandler(repository, CreateAccounts("ACC-001", "ACC-002"), new TransferFundsCommandValidator());

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(-500.00m, result.Value.Debit.Amount);
        Assert.Equal(500.00m, result.Value.Credit.Amount);
        Assert.Equal("ACC-001", result.Value.Debit.AccountId);
        Assert.Equal("ACC-002", result.Value.Credit.AccountId);
        // Each leg needs its own ExternalRef — both accounts would otherwise share one
        // (AccountId, ExternalRef) idempotency key derived from the same command.
        Assert.Equal("MOV-20240715-01-DR", result.Value.Debit.ExternalRef);
        Assert.Equal("MOV-20240715-01-CR", result.Value.Credit.ExternalRef);
    }

    [Fact]
    public async Task Handle_UnknownFromAccount_ReturnsNotFoundFailure()
    {
        var repository = new FakeMovementRepository();
        var handler = new TransferFundsCommandHandler(repository, CreateAccounts("ACC-002"), new TransferFundsCommandValidator());

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("account.not_found", result.Error.Code);
    }

    [Fact]
    public async Task Handle_UnknownToAccount_ReturnsNotFoundFailure()
    {
        var repository = new FakeMovementRepository();
        var handler = new TransferFundsCommandHandler(repository, CreateAccounts("ACC-001"), new TransferFundsCommandValidator());

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("account.not_found", result.Error.Code);
    }

    [Fact]
    public async Task Handle_SameFromAndToAccount_ReturnsValidationFailure()
    {
        var repository = new FakeMovementRepository();
        var handler = new TransferFundsCommandHandler(repository, CreateAccounts("ACC-001"), new TransferFundsCommandValidator());
        var command = CreateCommand() with { ToAccountId = "ACC-001" };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("transfer.invalid", result.Error.Code);
    }

    [Fact]
    public async Task Handle_CurrencyMismatchWithFromAccount_ReturnsCurrencyInvalidFailure()
    {
        var repository = new FakeMovementRepository();
        var accounts = new FakeAccountRepository();
        accounts.AddExisting("ACC-001", currency: "USD");
        accounts.AddExisting("ACC-002");
        var handler = new TransferFundsCommandHandler(repository, accounts, new TransferFundsCommandValidator());

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("movement.currency.invalid", result.Error.Code);
    }

    [Fact]
    public async Task Handle_CurrencyMismatchWithToAccount_ReturnsCurrencyInvalidFailure()
    {
        var repository = new FakeMovementRepository();
        var accounts = new FakeAccountRepository();
        accounts.AddExisting("ACC-001");
        accounts.AddExisting("ACC-002", currency: "USD");
        var handler = new TransferFundsCommandHandler(repository, accounts, new TransferFundsCommandValidator());

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("movement.currency.invalid", result.Error.Code);
    }
}
