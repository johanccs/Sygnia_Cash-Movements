namespace Sygnia.Domain.Models;

/// <summary>
/// A single cash movement against an account. <c>(AccountId, ExternalRef)</c> is both the
/// composite primary key and the idempotency key — enforced by the database, not this class.
/// </summary>
public sealed class Movement
{
    public const int AccountIdMaxLength = 10;
    public const int ExternalRefMaxLength = 20;
    public const int NarrationMaxLength = 200;
    public const int MovedByMaxLength = 50;

    public Movement(
        string accountId,
        string externalRef,
        string currency,
        decimal amount,
        DateTime occurredAt,
        string? narration,
        Guid refNr,
        string movedBy,
        DateTime movedDate)
    {
        Guard.AgainstNullOrWhiteSpace(accountId, nameof(accountId));
        Guard.AgainstTooLong(accountId, AccountIdMaxLength, nameof(accountId));

        Guard.AgainstNullOrWhiteSpace(externalRef, nameof(externalRef));
        Guard.AgainstTooLong(externalRef, ExternalRefMaxLength, nameof(externalRef));

        Guard.AgainstInvalidCurrency(currency, nameof(currency));
        Guard.AgainstZero(amount, nameof(amount));
        Guard.AgainstNonUtcOrDefault(occurredAt, nameof(occurredAt));

        Guard.AgainstTooLong(narration, NarrationMaxLength, nameof(narration));
        Guard.AgainstEmptyGuid(refNr, nameof(refNr));

        Guard.AgainstNullOrWhiteSpace(movedBy, nameof(movedBy));
        Guard.AgainstTooLong(movedBy, MovedByMaxLength, nameof(movedBy));

        Guard.AgainstNonUtcOrDefault(movedDate, nameof(movedDate));

        AccountId = accountId;
        ExternalRef = externalRef;
        Currency = currency;
        Amount = amount;
        OccurredAt = occurredAt;
        Narration = narration;
        RefNr = refNr;
        MovedBy = movedBy;
        MovedDate = movedDate;
    }

    /// <summary>First half of the composite key. Example: <c>ACC-001</c>.</summary>
    public string AccountId { get; }

    /// <summary>
    /// Second half of the composite key, and the caller-supplied idempotency key.
    /// Example: <c>MOV-20240715-000123</c>.
    /// </summary>
    public string ExternalRef { get; }

    /// <summary>Three-letter ISO 4217 code, e.g. <c>ZAR</c>.</summary>
    public string Currency { get; }

    /// <summary>
    /// Signed amount, stored as <c>DECIMAL(19,4)</c>. Positive is a deposit, negative a
    /// withdrawal — never <c>float</c>, which cannot represent money exactly.
    /// </summary>
    public decimal Amount { get; }

    /// <summary>When the movement happened, in UTC.</summary>
    public DateTime OccurredAt { get; }

    /// <summary>Optional free text, e.g. "Initial deposit".</summary>
    public string? Narration { get; }

    public Guid RefNr { get; }

    public string MovedBy { get; }

    public DateTime MovedDate { get; }

    /// <summary>True for a deposit, false for a withdrawal. Zero is rejected at construction.</summary>
    public bool IsDeposit => Amount > 0m;
}
