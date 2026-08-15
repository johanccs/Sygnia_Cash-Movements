namespace Sygnia.Presentation.Mapping;

/// <summary>Converts between domain <see cref="Sygnia.Domain.Models.User"/> and the wire message.</summary>
internal static class UserProtoMapper
{
    public static User ToProto(this Sygnia.Domain.Models.User user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Surname = user.Surname,
    };
}
