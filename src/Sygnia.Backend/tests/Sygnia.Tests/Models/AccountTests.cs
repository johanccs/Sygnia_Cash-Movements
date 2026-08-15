using Sygnia.Domain.Models;

namespace Sygnia.Tests.Models;

public sealed class AccountTests
{
    private const string ValidAccountId = "ACC-001";
    private const string ValidAccountName = "Operations ZAR";
    private const string ValidCurrency = "ZAR";
    private const string ValidCreatedBy = "jsmith";

    private static readonly DateTime ValidCreatedDate =
        new(2024, 7, 15, 10, 42, 31, DateTimeKind.Utc);

    [Fact]
    public void Constructor_WithValidArguments_SetsEveryProperty()
    {
        var account = new Account(
            ValidAccountId, ValidAccountName, "Jane Doe", ValidCurrency, ValidCreatedDate, ValidCreatedBy);

        Assert.Equal(ValidAccountId, account.AccountId);
        Assert.Equal(ValidAccountName, account.AccountName);
        Assert.Equal("Jane Doe", account.ContactPerson);
        Assert.Equal(ValidCurrency, account.Currency);
        Assert.Equal(ValidCreatedDate, account.CreatedDate);
        Assert.Equal(ValidCreatedBy, account.CreatedBy);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingAccountId_Throws(string? accountId)
        => Assert.Throws<ArgumentException>(() => new Account(
            accountId!, ValidAccountName, null, ValidCurrency, ValidCreatedDate, ValidCreatedBy));

    [Fact]
    public void Constructor_WithAccountIdLongerThanTen_Throws()
        => Assert.Throws<ArgumentException>(() => new Account(
            "ACC-0000001", ValidAccountName, null, ValidCurrency, ValidCreatedDate, ValidCreatedBy));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingAccountName_Throws(string? accountName)
        => Assert.Throws<ArgumentException>(() => new Account(
            ValidAccountId, accountName!, null, ValidCurrency, ValidCreatedDate, ValidCreatedBy));

    [Fact]
    public void Constructor_WithAccountNameLongerThanTwenty_Throws()
        => Assert.Throws<ArgumentException>(() => new Account(
            ValidAccountId, new string('X', 21), null, ValidCurrency, ValidCreatedDate, ValidCreatedBy));

    [Fact]
    public void Constructor_WithNullContactPerson_IsAllowed()
        => Assert.Null(new Account(
            ValidAccountId, ValidAccountName, null, ValidCurrency, ValidCreatedDate, ValidCreatedBy).ContactPerson);

    [Fact]
    public void Constructor_WithContactPersonLongerThanThirty_Throws()
        => Assert.Throws<ArgumentException>(() => new Account(
            ValidAccountId, ValidAccountName, new string('X', 31), ValidCurrency, ValidCreatedDate, ValidCreatedBy));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ZA")]
    [InlineData("ZARS")]
    public void Constructor_WithInvalidCurrency_Throws(string? currency)
        => Assert.Throws<ArgumentException>(() => new Account(
            ValidAccountId, ValidAccountName, null, currency!, ValidCreatedDate, ValidCreatedBy));

    [Fact]
    public void Constructor_WithDefaultCreatedDate_Throws()
        => Assert.Throws<ArgumentException>(() => new Account(
            ValidAccountId, ValidAccountName, null, ValidCurrency, default, ValidCreatedBy));

    [Fact]
    public void Constructor_WithNonUtcCreatedDate_Throws()
        => Assert.Throws<ArgumentException>(() => new Account(
            ValidAccountId, ValidAccountName, null, ValidCurrency,
            new DateTime(2024, 7, 15, 10, 42, 31, DateTimeKind.Local), ValidCreatedBy));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingCreatedBy_Throws(string? createdBy)
        => Assert.Throws<ArgumentException>(() => new Account(
            ValidAccountId, ValidAccountName, null, ValidCurrency, ValidCreatedDate, createdBy!));

    [Fact]
    public void Constructor_WithCreatedByLongerThanFifty_Throws()
        => Assert.Throws<ArgumentException>(() => new Account(
            ValidAccountId, ValidAccountName, null, ValidCurrency, ValidCreatedDate, new string('X', 51)));
}
