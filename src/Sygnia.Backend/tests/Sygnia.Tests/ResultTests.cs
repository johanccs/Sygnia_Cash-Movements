using Sygnia.Domain;

namespace Sygnia.Tests;

/// <summary>
/// Result carries expected business failures as values, so the compiler forces the caller
/// to deal with them. Broken invariants stay in the constructors, where they throw.
/// </summary>
public sealed class ResultTests
{
    [Fact]
    public void Success_ExposesTheValue()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Failure_ExposesTheError()
    {
        var error = new Error("account.name.invalid", "Account name is required.");

        var result = Result<int>.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Value_OnAFailure_Throws()
    {
        var result = Result<int>.Failure(new Error("x", "y"));

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Error_OnASuccess_Throws()
    {
        var result = Result<int>.Success(1);

        Assert.Throws<InvalidOperationException>(() => result.Error);
    }

    [Fact]
    public void Failure_WithNullError_Throws()
        => Assert.Throws<ArgumentNullException>(() => Result<int>.Failure(null!));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Error_WithMissingCode_Throws(string? code)
        => Assert.Throws<ArgumentException>(() => new Error(code!, "message"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Error_WithMissingMessage_Throws(string? message)
        => Assert.Throws<ArgumentException>(() => new Error("code", message!));
}
