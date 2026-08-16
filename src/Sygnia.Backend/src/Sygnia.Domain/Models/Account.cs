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
        string currency,
        DateTime createdDate,
        string createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(accountId, nameof(accountId));
        Guard.AgainstTooLong(accountId, AccountIdMaxLength, nameof(accountId));

        Guard.AgainstNullOrWhiteSpace(accountName, nameof(accountName));
        Guard.AgainstTooLong(accountName, AccountNameMaxLength, nameof(accountName));

        Guard.AgainstTooLong(contactPerson, ContactPersonMaxLength, nameof(contactPerson));

        Guard.AgainstInvalidCurrency(currency, nameof(currency));

        Guard.AgainstNonUtcOrDefault(createdDate, nameof(createdDate));

        Guard.AgainstNullOrWhiteSpace(createdBy, nameof(createdBy));
        Guard.AgainstTooLong(createdBy, CreatedByMaxLength, nameof(createdBy));

        AccountId = accountId;
        AccountName = accountName;
        ContactPerson = contactPerson;
        Currency = Guard.NormalizeCurrency(currency);
        CreatedDate = createdDate;
        CreatedBy = createdBy;
    }

    /// <summary>Primary key. Example: <c>ACC-001</c>.</summary>
    public string AccountId { get; }

    public string AccountName { get; }

    /// <summary>Optional.</summary>
    public string? ContactPerson { get; }

    /// <summary>
    /// Three-letter ISO 4217 code, e.g. <c>ZAR</c>. Every movement against this account must
    /// match it, rejected as <c>movement.currency.invalid</c> if not — otherwise the balance
    /// <c>SUM</c> would silently mix currencies.
    /// </summary>
    public string Currency { get; }

    /// <summary>UTC.</summary>
    public DateTime CreatedDate { get; }

    public string CreatedBy { get; }

    /// <summary>
    /// Returns a copy of this account carrying a new name, or a failure describing why not.
    /// Properties are readonly, so an update produces a new instance rather than mutating this one.
    /// </summary>
    public Result<Account> WithAccountName(string accountName)
    {
        var error = ValidateAccountName(accountName);
        if (error is not null)
        {
            return Result<Account>.Failure(error);
        }

        return Result<Account>.Success(
            new Account(AccountId, accountName, ContactPerson, Currency, CreatedDate, CreatedBy));
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
            new Account(AccountId, AccountName, contactPerson, Currency, CreatedDate, CreatedBy));
    }

    /// <summary>
    /// Rejects a <paramref name="currency"/> that does not match this account's, comparing on
    /// the same normalised (uppercase) form the account itself was constructed with — so "zar"
    /// and "ZAR" are treated as identical. Shared by <c>SubmitMovementCommandHandler</c> and
    /// <c>TransferFundsCommandHandler</c> rather than each re-implementing the check, since the
    /// balance <c>SUM</c> would otherwise silently mix currencies on a mismatch.
    /// </summary>
    public Error? EnsureCurrencyMatches(string currency)
    {
        var normalized = Guard.NormalizeCurrency(currency);
        if (string.Equals(Currency, normalized, StringComparison.Ordinal))
        {
            return null;
        }

        return new Error(
            ErrorCode.MovementCurrencyInvalid,
            $"Account '{AccountId}' is '{Currency}'; amount was submitted in '{normalized}'.");
    }

    private static Error? ValidateAccountName(string? accountName) =>
        Guard.TryValidateLength(
            accountName, AccountNameMaxLength, required: true, ErrorCode.AccountNameInvalid, "Account name");

    private static Error? ValidateContactPerson(string? contactPerson) =>
        Guard.TryValidateLength(
            contactPerson, ContactPersonMaxLength, required: false, ErrorCode.AccountContactPersonInvalid, "Contact person");
}
