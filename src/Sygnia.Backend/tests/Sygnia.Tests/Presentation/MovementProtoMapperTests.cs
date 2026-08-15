using System.Globalization;
using Sygnia.Domain.Models;
using Sygnia.Presentation.Mapping;

namespace Sygnia.Tests.Presentation;

/// <summary>
/// Regression coverage for a real bug found via manual gRPC testing: an idempotent replay
/// returned a different Amount string ("12500.0000" vs "12500.00") for the identical value,
/// because decimal.ToString() without a fixed format preserves whichever scale the value
/// happens to carry — 2dp for a freshly constructed Movement, 4dp for one read back from the
/// DECIMAL(19,4) column. Same problem applies to balance and running-total formatting.
/// </summary>
public sealed class MovementProtoMapperTests
{
    private static readonly DateTime OccurredAt = new(2024, 7, 15, 10, 42, 31, DateTimeKind.Utc);

    [Theory]
    [InlineData("12500.00")] // as a caller might type it — 2 decimal places
    [InlineData("12500.0000")] // as EF Core reads DECIMAL(19,4) back — 4 decimal places
    [InlineData("12500")] // no fractional part at all
    public void ToProto_AnyInputScale_FormatsAmountWithFixedFourDecimalScale(string amountLiteral)
    {
        var movement = new Movement(
            "ACC-001",
            "MOV-20240715-000123",
            "ZAR",
            decimal.Parse(amountLiteral, CultureInfo.InvariantCulture),
            OccurredAt,
            null,
            Guid.NewGuid(),
            "jsmith",
            OccurredAt);

        var proto = movement.ToProto();

        Assert.Equal("12500.0000", proto.Amount);
    }

    [Fact]
    public void ToDecimalAmount_RoundTripsThroughToProto()
    {
        const string amount = "12500.0000";

        var roundTripped = amount.ToDecimalAmount();

        Assert.Equal(12500.0000m, roundTripped);
    }
}
