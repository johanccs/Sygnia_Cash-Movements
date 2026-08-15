using Sygnia.Domain.Models;
using Sygnia.Infrastructure.Entities;

namespace Sygnia.Infrastructure.Mapping;

internal static class MovementMapper
{
    public static MovementEntity ToEntity(this Movement movement) => new()
    {
        AccountId = movement.AccountId,
        ExternalRef = movement.ExternalRef,
        Currency = movement.Currency,
        Amount = movement.Amount,
        OccurredAt = movement.OccurredAt,
        Narration = movement.Narration,
        RefNr = movement.RefNr,
        MovedBy = movement.MovedBy,
        MovedDate = movement.MovedDate,
    };

    public static Movement ToDomain(this MovementEntity entity) => new(
        entity.AccountId,
        entity.ExternalRef,
        entity.Currency,
        entity.Amount,
        entity.OccurredAt,
        entity.Narration,
        entity.RefNr,
        entity.MovedBy,
        entity.MovedDate);
}
