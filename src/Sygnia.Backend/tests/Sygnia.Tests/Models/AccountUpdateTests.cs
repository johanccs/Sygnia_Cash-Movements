using Sygnia.Domain.Models;

namespace Sygnia.Tests.Models;

/// <summary>
/// Account properties are readonly, so an "update" produces a new Account rather than
/// mutating this one. And these are methods, not constructors, so an invalid value comes
/// back as a Result the caller must handle — it does not throw.
/// </summary>
public sealed class AccountUpdateTests
{
    private static readonly DateTime CreatedOn =
        new(2024, 7, 15, 10, 42, 31, DateTimeKind.Utc);

    private static Account CreateValid() =>
        new("ACC-001", "Operations ZAR", "Jane Doe", CreatedOn, "jsmith");

    // --- WithAccountName ---

    [Fact]
    public void WithAccountName_WhenValid_ReturnsANewAccountCarryingTheNewName()
    {
        var original = CreateValid();

        var result = original.WithAccountName("Treasury ZAR");

        Assert.True(result.IsSuccess);
        Assert.Equal("Treasury ZAR", result.Value.AccountName);
    }

    [Fact]
    public void WithAccountName_DoesNotMutateTheOriginal()
    {
        var original = CreateValid();

        original.WithAccountName("Treasury ZAR");

        Assert.Equal("Operations ZAR", original.AccountName);
    }

    [Fact]
    public void WithAccountName_PreservesEveryOtherProperty()
    {
        var original = CreateValid();

        var updated = original.WithAccountName("Treasury ZAR").Value;

        Assert.Equal(original.AccountId, updated.AccountId);
        Assert.Equal(original.ContactPerson, updated.ContactPerson);
        Assert.Equal(original.CreatedDate, updated.CreatedDate);
        Assert.Equal(original.CreatedBy, updated.CreatedBy);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithAccountName_WhenMissing_ReturnsFailureAndDoesNotThrow(string? accountName)
    {
        var result = CreateValid().WithAccountName(accountName!);

        Assert.True(result.IsFailure);
        Assert.Equal("account.name.invalid", result.Error.Code);
    }

    [Fact]
    public void WithAccountName_WhenTooLong_ReturnsFailure()
    {
        var result = CreateValid().WithAccountName(new string('X', 21));

        Assert.True(result.IsFailure);
        Assert.Equal("account.name.invalid", result.Error.Code);
    }

    // --- WithContactPerson ---

    [Fact]
    public void WithContactPerson_WhenValid_ReturnsANewAccountCarryingTheNewContact()
    {
        var result = CreateValid().WithContactPerson("John Roe");

        Assert.True(result.IsSuccess);
        Assert.Equal("John Roe", result.Value.ContactPerson);
    }

    [Fact]
    public void WithContactPerson_WhenNull_ClearsIt()
    {
        var result = CreateValid().WithContactPerson(null);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.ContactPerson);
    }

    [Fact]
    public void WithContactPerson_DoesNotMutateTheOriginal()
    {
        var original = CreateValid();

        original.WithContactPerson("John Roe");

        Assert.Equal("Jane Doe", original.ContactPerson);
    }

    [Fact]
    public void WithContactPerson_PreservesEveryOtherProperty()
    {
        var original = CreateValid();

        var updated = original.WithContactPerson("John Roe").Value;

        Assert.Equal(original.AccountId, updated.AccountId);
        Assert.Equal(original.AccountName, updated.AccountName);
        Assert.Equal(original.CreatedDate, updated.CreatedDate);
        Assert.Equal(original.CreatedBy, updated.CreatedBy);
    }

    [Fact]
    public void WithContactPerson_WhenTooLong_ReturnsFailure()
    {
        var result = CreateValid().WithContactPerson(new string('X', 31));

        Assert.True(result.IsFailure);
        Assert.Equal("account.contactperson.invalid", result.Error.Code);
    }
}
