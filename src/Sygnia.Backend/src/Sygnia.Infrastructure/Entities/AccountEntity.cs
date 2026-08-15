namespace Sygnia.Infrastructure.Entities;

public sealed class AccountEntity
{
    public required string AccountId { get; set; }

    public required string AccountName { get; set; }

    public string? ContactPerson { get; set; }

    public DateTime CreatedDate { get; set; }

    public required string CreatedBy { get; set; }

    public ICollection<MovementEntity> Movements { get; set; } = new List<MovementEntity>();
}
