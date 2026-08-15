using Sygnia.Domain.Models;
using Sygnia.Infrastructure.Entities;

namespace Sygnia.Infrastructure.Mapping;

internal static class AccountMapper
{
    public static AccountEntity ToEntity(this Account account) => new()
    {
        AccountId = account.AccountId,
        AccountName = account.AccountName,
        ContactPerson = account.ContactPerson,
        Currency = account.Currency,
        CreatedDate = account.CreatedDate,
        CreatedBy = account.CreatedBy,
    };

    public static Account ToDomain(this AccountEntity entity) => new(
        entity.AccountId,
        entity.AccountName,
        entity.ContactPerson,
        entity.Currency,
        entity.CreatedDate,
        entity.CreatedBy);
}
