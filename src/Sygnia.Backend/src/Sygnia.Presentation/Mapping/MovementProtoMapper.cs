using System.Globalization;
using Google.Protobuf.WellKnownTypes;
using Sygnia.Domain.Models;

namespace Sygnia.Presentation.Mapping;

/// <summary>
/// Converts between domain <see cref="Movement"/> and the wire messages. Amount crosses as a
/// string decimal, never a double — protobuf has no decimal type and double corrupts cents.
/// </summary>
internal static class MovementProtoMapper
{
    /// <summary>
    /// Matches the <c>DECIMAL(19,4)</c> column scale. Formatting with a fixed scale, rather
    /// than <c>decimal.ToString()</c>'s variable scale, matters because the same logical
    /// amount can otherwise render differently depending on whether it came from a freshly
    /// constructed <see cref="Movement"/> (e.g. "12500.00") or one read back from the database
    /// (e.g. "12500.0000") — an idempotent replay would then return a different string for an
    /// identical amount, which defeats the point of "identical response" idempotency.
    /// </summary>
    private const string AmountFormat = "F4";

    /// <summary>Formats any wire-bound amount (movement, balance, running total) consistently.</summary>
    public static string ToAmountString(this decimal amount) => amount.ToString(AmountFormat, CultureInfo.InvariantCulture);

    public static Movement ToProto(this Sygnia.Domain.Models.Movement movement) => new()
    {
        AccountId = movement.AccountId,
        ExternalRef = movement.ExternalRef,
        Currency = movement.Currency,
        Amount = movement.Amount.ToAmountString(),
        OccurredAt = Timestamp.FromDateTime(movement.OccurredAt),
        Narration = movement.Narration ?? string.Empty,
        RefNr = movement.RefNr.ToString(),
        MovedBy = movement.MovedBy,
        MovedDate = Timestamp.FromDateTime(movement.MovedDate),
    };

    public static decimal ToDecimalAmount(this string amount) =>
        decimal.Parse(amount, NumberStyles.Number, CultureInfo.InvariantCulture);
}
