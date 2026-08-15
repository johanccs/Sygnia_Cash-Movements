using Sygnia.Domain.Models;
using Sygnia.Infrastructure.Mapping;

namespace Sygnia.Tests.Infrastructure.Unit;

/// <summary>
/// A silent field-mapping bug (e.g. swapped columns, a dropped property) would corrupt every
/// row written or read through the repositories, without a compile error to catch it.
/// </summary>
public sealed class MovementMapperTests
{
    [Fact]
    public void ToEntity_ThenToDomain_RoundTripsEveryField()
    {
        var refNr = Guid.NewGuid();
        var occurredAt = new DateTime(2024, 7, 15, 10, 42, 31, DateTimeKind.Utc);
        var movement = new Movement(
            "ACC-001",
            "MOV-20240715-000123",
            "ZAR",
            12500.00m,
            occurredAt,
            "Initial deposit",
            refNr,
            "jsmith",
            occurredAt);

        var roundTripped = movement.ToEntity().ToDomain();

        Assert.Equal(movement.AccountId, roundTripped.AccountId);
        Assert.Equal(movement.ExternalRef, roundTripped.ExternalRef);
        Assert.Equal(movement.Currency, roundTripped.Currency);
        Assert.Equal(movement.Amount, roundTripped.Amount);
        Assert.Equal(movement.OccurredAt, roundTripped.OccurredAt);
        Assert.Equal(movement.Narration, roundTripped.Narration);
        Assert.Equal(movement.RefNr, roundTripped.RefNr);
        Assert.Equal(movement.MovedBy, roundTripped.MovedBy);
        Assert.Equal(movement.MovedDate, roundTripped.MovedDate);
    }
}
