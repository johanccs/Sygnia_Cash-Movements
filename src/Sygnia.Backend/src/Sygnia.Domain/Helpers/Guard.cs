namespace Sygnia.Domain;

/// <summary>
/// Guard clauses shared by the domain models. These throw rather than returning a
/// <c>Result</c> — a broken invariant must never produce a half-built object.
/// </summary>
internal static class Guard
{
    internal static void AgainstNullOrWhiteSpace(string? value, string paramName)
    {
        var message = RequiredMessage(value, paramName);
        if (message is not null)
        {
            throw new ArgumentException(message, paramName);
        }
    }

    internal static void AgainstTooLong(string? value, int maxLength, string paramName)
    {
        var message = TooLongMessage(value, maxLength, paramName);
        if (message is not null)
        {
            throw new ArgumentException(message, paramName);
        }
    }

    private static string? RequiredMessage(string? value, string label) =>
        string.IsNullOrWhiteSpace(value) ? $"{label} is required." : null;

    private static string? TooLongMessage(string? value, int maxLength, string label) =>
        value is not null && value.Length > maxLength
            ? $"{label} must be {maxLength} characters or fewer, but was {value.Length}."
            : null;

    /// <summary>ISO 4217: exactly three letters, e.g. ZAR.</summary>
    internal static void AgainstInvalidCurrency(string? value, string paramName)
    {
        if (value is null || value.Length != 3 || !value.All(char.IsLetter))
        {
            throw new ArgumentException(
                $"{paramName} must be a three-letter ISO 4217 code, e.g. ZAR.", paramName);
        }
    }

    /// <summary>
    /// Currency codes are compared and stored case-insensitively but normalised to a single
    /// canonical form, so "zar" and "ZAR" are the same currency everywhere: at construction
    /// (<see cref="Models.Account"/>, <see cref="Models.Movement"/>) and at comparison
    /// (<see cref="Models.Account.EnsureCurrencyMatches"/>).
    /// </summary>
    internal static string NormalizeCurrency(string value) => value.ToUpperInvariant();

    /// <summary>
    /// Timestamps cross the gRPC wire as UTC. Accepting a local or unspecified DateTime here
    /// would let a local time be stored as though it were UTC — a silent, hard-to-trace defect.
    /// </summary>
    internal static void AgainstNonUtcOrDefault(DateTime value, string paramName)
    {
        if (value == default)
        {
            throw new ArgumentException($"{paramName} is required.", paramName);
        }

        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                $"{paramName} must be UTC, but its Kind was {value.Kind}.", paramName);
        }
    }

    internal static void AgainstEmptyGuid(Guid value, string paramName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException($"{paramName} is required.", paramName);
        }
    }

    /// <summary>
    /// Sign carries meaning — positive is a deposit, negative a withdrawal — so zero is not a
    /// small movement, it is an absent one.
    /// </summary>
    internal static void AgainstZero(decimal value, string paramName)
    {
        if (value == 0m)
        {
            throw new ArgumentOutOfRangeException(
                paramName, value, $"{paramName} must be non-zero: positive for a deposit, negative for a withdrawal.");
        }
    }

    /// <summary>
    /// The <c>Result</c>-returning counterpart of <see cref="AgainstNullOrWhiteSpace"/> +
    /// <see cref="AgainstTooLong"/>, for the update methods (e.g.
    /// <see cref="Models.Account.WithAccountName"/>) that must return a failure rather than
    /// throw. Shares <see cref="RequiredMessage"/>/<see cref="TooLongMessage"/> with those two
    /// so the rules cannot drift apart.
    /// </summary>
    internal static Error? TryValidateLength(
        string? value, int maxLength, bool required, ErrorCode code, string fieldLabel)
    {
        var message = (required ? RequiredMessage(value, fieldLabel) : null)
            ?? TooLongMessage(value, maxLength, fieldLabel);
        return message is not null ? new Error(code, message) : null;
    }
}
