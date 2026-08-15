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

    /// <summary>
    /// Returns a copy of this account carrying a new name, or a failure describing why not.
    /// <para>
    /// The properties are readonly, so there is nothing to mutate — an update produces a new
    /// instance and leaves this one untouched. And because this is a method rather than a
    /// constructor, an invalid name is an expected outcome the caller must handle, not an
    /// exception.
    /// </para>
    /// </summary>
    public Result<Account> WithAccountName(string accountName)
    {
        var error = ValidateAccountName(accountName);
        if (error is not null)
        {
            return Result<Account>.Failure(error);
        }

        return Result<Account>.Success(
            new Account(AccountId, accountName, ContactPerson, CreatedDate, CreatedBy));
    }

    /// <summary>
    /// Returns a copy of this account carrying a new contact person, or a failure describing
    /// why not. Passing <c>null</c> clears the contact, which is valid — the column is optional.
    /// </summary>
    public Result<Account> WithContactPerson(string? contactPerson)
    {
        var error = ValidateContactPerson(contactPerson);
        if (error is not null)
        {
            return Result<Account>.Failure(error);
        }

        return Result<Account>.Success(
            new Account(AccountId, AccountName, contactPerson, CreatedDate, CreatedBy));
    }

    private static Error? ValidateAccountName(string? accountName)
    {
        if (string.IsNullOrWhiteSpace(accountName))
        {
            return new Error("account.name.invalid", "Account name is required.");
        }

        if (accountName.Length > AccountNameMaxLength)
        {
            return new Error(
                "account.name.invalid",
                $"Account name must be {AccountNameMaxLength} characters or fewer, but was {accountName.Length}.");
        }

        return null;
    }

    private static Error? ValidateContactPerson(string? contactPerson)
    {
        if (contactPerson is not null && contactPerson.Length > ContactPersonMaxLength)
        {
            return new Error(
                "account.contactperson.invalid",
                $"Contact person must be {ContactPersonMaxLength} characters or fewer, but was {contactPerson.Length}.");
        }

        return null;
    }
}
