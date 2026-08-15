using Sygnia.Domain.Models;

namespace Sygnia.Tests.Models;

public sealed class UserTests
{
    private const string ValidId = "jsmith";
    private const string ValidName = "Jane";
    private const string ValidSurname = "Smith";

    [Fact]
    public void Constructor_WithValidArguments_SetsEveryProperty()
    {
        var user = new User(ValidId, ValidName, ValidSurname);

        Assert.Equal(ValidId, user.Id);
        Assert.Equal(ValidName, user.Name);
        Assert.Equal(ValidSurname, user.Surname);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingId_Throws(string? id)
        => Assert.Throws<ArgumentException>(() => new User(id!, ValidName, ValidSurname));

    [Fact]
    public void Constructor_WithIdLongerThanFifty_Throws()
        => Assert.Throws<ArgumentException>(() => new User(new string('X', 51), ValidName, ValidSurname));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingName_Throws(string? name)
        => Assert.Throws<ArgumentException>(() => new User(ValidId, name!, ValidSurname));

    [Fact]
    public void Constructor_WithNameLongerThanFifty_Throws()
        => Assert.Throws<ArgumentException>(() => new User(ValidId, new string('X', 51), ValidSurname));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingSurname_Throws(string? surname)
        => Assert.Throws<ArgumentException>(() => new User(ValidId, ValidName, surname!));

    [Fact]
    public void Constructor_WithSurnameLongerThanFifty_Throws()
        => Assert.Throws<ArgumentException>(() => new User(ValidId, ValidName, new string('X', 51)));

    [Fact]
    public void FullName_JoinsNameAndSurname()
        => Assert.Equal("Jane Smith", new User(ValidId, ValidName, ValidSurname).FullName);
}
