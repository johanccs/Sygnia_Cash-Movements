using Sygnia.Domain.Models;
using Sygnia.Infrastructure.Entities;

namespace Sygnia.Infrastructure.Mapping;

internal static class UserMapper
{
    public static UserEntity ToEntity(this User user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Surname = user.Surname,
    };

    public static User ToDomain(this UserEntity entity) => new(
        entity.Id,
        entity.Name,
        entity.Surname);
}
