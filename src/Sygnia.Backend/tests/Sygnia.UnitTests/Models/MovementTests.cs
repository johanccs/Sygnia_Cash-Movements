using Sygnia.Domain.Models;

namespace Sygnia.UnitTests.Models;

/// <summary>
/// A Movement that exists must be valid — that is the whole point of validating in the
/// constructor. These tests pin the guards so no later refactor can quietly relax them.
/// </summary>
public sealed class MovementTests
{
    private const string ValidAccountId = "ACC-001";
    private const string ValidExternalRef = "MOV-20240715-000123";
    private const string ValidCurrency = "ZAR";
    private const decimal ValidAmount = 12500.00m;
    private const string ValidMovedBy = "jsmith";

    private static readonly DateTime ValidOccurredAt =
        new(2024, 7, 15, 10, 42, 31, DateTimeKind.Utc);

    private static Movement CreateValid() => new(
        ValidAccountId,
        ValidExternalRef,
        ValidCurrency,
        ValidAmount,
        ValidOccurredAt,
        "Initial deposit",
        Guid.NewGuid(),
        ValidMovedBy,
        ValidOccurredAt);

    [Fact]
    public void Constructor_WithValidArguments_SetsEveryProperty()
    {
        var refNr = Guid.NewGuid();

        var movement = new Movement(
            ValidAccountId,
            ValidExternalRef,
            ValidCurrency,
            ValidAmount,
            ValidOccurredAt,
            "Initial deposit",
            refNr,
            ValidMovedBy,
            ValidOccurredAt);

        Assert.Equal(ValidAccountId, movement.AccountId);
        Assert.Equal(ValidExternalRef, movement.ExternalRef);
        Assert.Equal(ValidCurrency, movement.Currency);
        Assert.Equal(ValidAmount, movement.Amount);
        Assert.Equal(ValidOccurredAt, movement.OccurredAt);
        Assert.Equal("Initial deposit", movement.Narration);
        Assert.Equal(refNr, movement.RefNr);
        Assert.Equal(ValidMovedBy, movement.MovedBy);
    }

    // --- AccountId: first half of the composite key ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingAccountId_Throws(string? accountId)
        => Assert.Throws<ArgumentException>(() => new Movement(
            accountId!, ValidExternalRef, ValidCurrency, ValidAmount, ValidOccurredAt,
            null, Guid.NewGuid(), ValidMovedBy, ValidOccurredAt));

    [Fact]
    public void Constructor_WithAccountIdLongerThanTen_Throws()
        => Assert.Throws<ArgumentException>(() => new Movement(
            "ACC-0000001", ValidExternalRef, ValidCurrency, ValidAmount, ValidOccurredAt,
            null, Guid.NewGuid(), ValidMovedBy, ValidOccurredAt));

    // --- ExternalRef: second half of the composite key, and the idempotency key ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingExternalRef_Throws(string? externalRef)
        => Assert.Throws<ArgumentException>(() => new Movement(
            ValidAccountId, externalRef!, ValidCurrency, ValidAmount, ValidOccurredAt,
            null, Guid.NewGuid(), ValidMovedBy, ValidOccurredAt));

    [Fact]
    public void Constructor_WithExternalRefLongerThanTwenty_Throws()
        => Assert.Throws<ArgumentException>(() => new Movement(
            ValidAccountId, new string('X', 21), ValidCurrency, ValidAmount, ValidOccurredAt,
            null, Guid.NewGuid(), ValidMovedBy, ValidOccurredAt));

    // --- Currency: ISO 4217, exactly three letters ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ZA")]
    [InlineData("ZARS")]
    [InlineData("Z4R")]
    public void Constructor_WithInvalidCurrency_Throws(string? currency)
        => Assert.Throws<ArgumentException>(() => new Movement(
            ValidAccountId, ValidExternalRef, currency!, ValidAmount, ValidOccurredAt,
            null, Guid.NewGuid(), ValidMovedBy, ValidOccurredAt));

    // --- Amount: sign carries meaning, so zero is meaningless ---

    [Fact]
    public void Constructor_WithZeroAmount_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new Movement(
            ValidAccountId, ValidExternalRef, ValidCurrency, 0m, ValidOccurredAt,
            null, Guid.NewGuid(), ValidMovedBy, ValidOccurredAt));

    [Fact]
    public void Constructor_WithNegativeAmount_IsAWithdrawalAndIsAllowed()
    {
        var movement = new Movement(
            ValidAccountId, ValidExternalRef, ValidCurrency, -500.25m, ValidOccurredAt,
            null, Guid.NewGuid(), ValidMovedBy, ValidOccurredAt);

        Assert.Equal(-500.25m, movement.Amount);
    }

    // --- OccurredAt: timestamps cross the wire as UTC, so a local time is a bug ---

    [Fact]
    public void Constructor_WithDefaultOccurredAt_Throws()
        => Assert.Throws<ArgumentException>(() => new Movement(
            ValidAccountId, ValidExternalRef, ValidCurrency, ValidAmount, default,
            null, Guid.NewGuid(), ValidMovedBy, ValidOccurredAt));

    [Fact]
    public void Constructor_WithNonUtcOccurredAt_Throws()
        => Assert.Throws<ArgumentException>(() => new Movement(
            ValidAccountId, ValidExternalRef, ValidCurrency, ValidAmount,
            new DateTime(2024, 7, 15, 10, 42, 31, DateTimeKind.Local),
            null, Guid.NewGuid(), ValidMovedBy, ValidOccurredAt));

    // --- RefNr ---

    [Fact]
    public void Constructor_WithEmptyRefNr_Throws()
        => Assert.Throws<ArgumentException>(() => new Movement(
            ValidAccountId, ValidExternalRef, ValidCurrency, ValidAmount, ValidOccurredAt,
            null, Guid.Empty, ValidMovedBy, ValidOccurredAt));

    // --- Narration: optional, but bounded ---

    [Fact]
    public void Constructor_WithNullNarration_IsAllowed()
        => Assert.Null(new Movement(
            ValidAccountId, ValidExternalRef, ValidCurrency, ValidAmount, ValidOccurredAt,
            null, Guid.NewGuid(), ValidMovedBy, ValidOccurredAt).Narration);

    [Fact]
    public void Constructor_WithNarrationLongerThanTwoHundred_Throws()
        => Assert.Throws<ArgumentException>(() => new Movement(
            ValidAccountId, ValidExternalRef, ValidCurrency, ValidAmount, ValidOccurredAt,
            new string('X', 201), Guid.NewGuid(), ValidMovedBy, ValidOccurredAt));

    // --- MovedBy ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingMovedBy_Throws(string? movedBy)
        => Assert.Throws<ArgumentException>(() => new Movement(
            ValidAccountId, ValidExternalRef, ValidCurrency, ValidAmount, ValidOccurredAt,
            null, Guid.NewGuid(), movedBy!, ValidOccurredAt));

    [Fact]
    public void IsDeposit_ReflectsTheSignOfTheAmount()
    {
        Assert.True(CreateValid().IsDeposit);

        var withdrawal = new Movement(
            ValidAccountId, ValidExternalRef, ValidCurrency, -1m, ValidOccurredAt,
            null, Guid.NewGuid(), ValidMovedBy, ValidOccurredAt);

        Assert.False(withdrawal.IsDeposit);
    }
}
