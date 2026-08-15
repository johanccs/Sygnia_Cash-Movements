namespace Sygnia.Infrastructure.Entities;

/// <summary>
/// EF Core mapping for a movement row. A plain data-shape, not a domain object — no guard
/// clauses here; validity is enforced when a <see cref="Sygnia.Domain.Models.Movement"/> is
/// constructed, before it ever reaches this layer.
/// </summary>
public sealed class MovementEntity
{
    public required string AccountId { get; set; }

    public required string ExternalRef { get; set; }

    public required string Currency { get; set; }

    public decimal Amount { get; set; }

    public DateTime OccurredAt { get; set; }

    public string? Narration { get; set; }

    public Guid RefNr { get; set; }

    public required string MovedBy { get; set; }

    public DateTime MovedDate { get; set; }

    public AccountEntity? Account { get; set; }
}
