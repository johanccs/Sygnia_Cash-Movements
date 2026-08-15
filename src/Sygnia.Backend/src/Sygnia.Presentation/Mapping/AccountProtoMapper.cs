using Google.Protobuf.WellKnownTypes;

namespace Sygnia.Presentation.Mapping;

/// <summary>Converts between domain <see cref="Sygnia.Domain.Models.Account"/> and the wire message.</summary>
internal static class AccountProtoMapper
{
    public static Account ToProto(this Sygnia.Domain.Models.Account account) => new()
    {
        AccountId = account.AccountId,
        AccountName = account.AccountName,
        ContactPerson = account.ContactPerson ?? string.Empty,
        Currency = account.Currency,
        CreatedDate = Timestamp.FromDateTime(account.CreatedDate),
        CreatedBy = account.CreatedBy,
    };
}
