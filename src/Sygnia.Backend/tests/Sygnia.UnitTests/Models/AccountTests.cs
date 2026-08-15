using Sygnia.Domain.Models;

namespace Sygnia.UnitTests.Models;

public sealed class AccountTests
{
    private const string ValidAccountId = "ACC-001";
    private const string ValidAccountName = "Operations ZAR";
    private const string ValidCreatedBy = "jsmith";

    private static readonly DateTime ValidCreatedDate =
        new(2024, 7, 15, 10, 42, 31, DateTimeKind.Utc);

    [Fact]
    public void Constructor_WithValidArguments_SetsEveryProperty()
    {
        var account = new Account(
            ValidAccountId, ValidAccountName, "Jane Doe", ValidCreatedDate, ValidCreatedBy);

        Assert.Equal(ValidAccountId, account.AccountId);
        Assert.Equal(ValidAccountName, account.AccountName);
        Assert.Equal("Jane Doe", account.ContactPerson);
        Assert.Equal(ValidCreatedDate, account.CreatedDate);
        Assert.Equal(ValidCreatedBy, account.CreatedBy);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingAccountId_Throws(string? accountId)
        => Assert.Throws<ArgumentException>(() => new Account(
            accountId!, ValidAccountName, null, ValidCreatedDate, ValidCreatedBy));

    [Fact]
    public void Constructor_WithAccountIdLongerThanTen_Throws()
        => Assert.Throws<ArgumentException>(() => new Account(
            "ACC-0000001", ValidAccountName, null, ValidCreatedDate, ValidCreatedBy));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingAccountName_Throws(string? accountName)
        => Assert.Throws<ArgumentException>(() => new Account(
            ValidAccountId, accountName!, null, ValidCreatedDate, ValidCreatedBy));

    [Fact]
    public void Constructor_WithAccountNameLongerThanTwenty_Throws()
        => Assert.Throws<ArgumentException>(() => new Account(
            ValidAccountId, new string('X', 21), null, ValidCreatedDate, ValidCreatedBy));

    [Fact]
    public void Constructor_WithNullContactPerson_IsAllowed()
        => Assert.Null(new Account(
            ValidAccountId, ValidAccountName, null, ValidCreatedDate, ValidCreatedBy).ContactPerson);

    [Fact]
    public void Constructor_WithContactPersonLongerThanThirty_Throws()
        => Assert.Throws<ArgumentException>(() => new Account(
            ValidAccountId, ValidAccountName, new string('X', 31), ValidCreatedDate, ValidCreatedBy));

    [Fact]
    public void Constructor_WithDefaultCreatedDate_Throws()
        => Assert.Throws<ArgumentException>(() => new Account(
            ValidAccountId, ValidAccountName, null, default, ValidCreatedBy));

    [Fact]
    public void Constructor_WithNonUtcCreatedDate_Throws()
        => Assert.Throws<ArgumentException>(() => new Account(
            ValidAccountId, ValidAccountName, null,
            new DateTime(2024, 7, 15, 10, 42, 31, DateTimeKind.Local), ValidCreatedBy));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingCreatedBy_Throws(string? createdBy)
        => Assert.Throws<ArgumentException>(() => new Account(
            ValidAccountId, ValidAccountName, null, ValidCreatedDate, createdBy!));

    [Fact]
    public void Constructor_WithCreatedByLongerThanFifty_Throws()
        => Assert.Throws<ArgumentException>(() => new Account(
            ValidAccountId, ValidAccountName, null, ValidCreatedDate, new string('X', 51)));
}
