namespace Sygnia.Infrastructure.Entities;

/// <summary>
/// One row per genuine idempotency-key conflict — same (AccountId, ExternalRef), different
/// Amount/Currency/OccurredAt. Written by <see cref="Sygnia.Infrastructure.Repositories.MovementRepository"/>
/// alongside the <c>ALREADY_EXISTS</c> result returned to the caller, so a conflict is never
/// silent: it is both reported to the caller and persisted here for investigation. An identical
/// replay (safe retry, same fields) is not a conflict and is never audited.
/// </summary>
public sealed class MovementConflictAuditEntity
{
    public int Id { get; set; }

    public required string AccountId { get; set; }

    public required string ExternalRef { get; set; }

    public decimal AttemptedAmount { get; set; }

    public required string AttemptedCurrency { get; set; }

    public DateTime AttemptedOccurredAt { get; set; }

    public decimal StoredAmount { get; set; }

    public required string StoredCurrency { get; set; }

    public DateTime StoredOccurredAt { get; set; }

    public required string ConflictingFields { get; set; }

    public DateTime DetectedAt { get; set; }
}
