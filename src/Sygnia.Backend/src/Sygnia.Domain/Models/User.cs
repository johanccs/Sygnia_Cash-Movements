namespace Sygnia.Domain.Models;

/// <summary>
/// An operations user. Their id is what lands in <c>MovedBy</c> and <c>CreatedBy</c>.
/// </summary>
public sealed class User
{
    public const int IdMaxLength = 50;
    public const int NameMaxLength = 50;
    public const int SurnameMaxLength = 50;

    public User(string id, string name, string surname)
    {
        Guard.AgainstNullOrWhiteSpace(id, nameof(id));
        Guard.AgainstTooLong(id, IdMaxLength, nameof(id));

        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        Guard.AgainstTooLong(name, NameMaxLength, nameof(name));

        Guard.AgainstNullOrWhiteSpace(surname, nameof(surname));
        Guard.AgainstTooLong(surname, SurnameMaxLength, nameof(surname));

        Id = id;
        Name = name;
        Surname = surname;
    }

    public string Id { get; }

    public string Name { get; }

    public string Surname { get; }

    public string FullName => $"{Name} {Surname}";
}
