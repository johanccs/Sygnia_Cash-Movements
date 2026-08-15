namespace Sygnia.Domain.Models;

/// <summary>
/// An account movements are recorded against. One customer may own several.
/// </summary>
public sealed class Account
{
    public const int AccountIdMaxLength = 10;
    public const int AccountNameMaxLength = 20;
    public const int ContactPersonMaxLength = 30;
    public const int CreatedByMaxLength = 50;

    public Account(
        string accountId,
        string accountName,
        string? contactPerson,
        DateTime createdDate,
        string createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(accountId, nameof(accountId));
        Guard.AgainstTooLong(accountId, AccountIdMaxLength, nameof(accountId));

        Guard.AgainstNullOrWhiteSpace(accountName, nameof(accountName));
        Guard.AgainstTooLong(accountName, AccountNameMaxLength, nameof(accountName));

        Guard.AgainstTooLong(contactPerson, ContactPersonMaxLength, nameof(contactPerson));

        Guard.AgainstNonUtcOrDefault(createdDate, nameof(createdDate));

        Guard.AgainstNullOrWhiteSpace(createdBy, nameof(createdBy));
        Guard.AgainstTooLong(createdBy, CreatedByMaxLength, nameof(createdBy));

        AccountId = accountId;
        AccountName = accountName;
        ContactPerson = contactPerson;
        CreatedDate = createdDate;
        CreatedBy = createdBy;
    }

    /// <summary>Primary key. Example: <c>ACC-001</c>.</summary>
    public string AccountId { get; }

    public string AccountName { get; }

    /// <summary>Optional.</summary>
    public string? ContactPerson { get; }

    /// <summary>UTC.</summary>
    public DateTime CreatedDate { get; }

    public string CreatedBy { get; }
}
