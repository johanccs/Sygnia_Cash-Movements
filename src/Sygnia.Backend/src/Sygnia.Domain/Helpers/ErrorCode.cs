namespace Sygnia.Domain;

/// <summary>
/// Enumerates every stable <see cref="Error.Code"/> string used across the solution, replacing
/// scattered magic-string literals at call sites. <see cref="ErrorCodeExtensions.ToCode"/> is
/// the single place the enum-to-wire-string convention lives, so a new code is added once and
/// reused everywhere instead of being retyped as a literal.
/// </summary>
public enum ErrorCode
{
    AccountAlreadyExists,
    AccountContactPersonInvalid,
    AccountInvalid,
    AccountNameInvalid,
    AccountNotFound,
    BalanceInvalid,
    MovementAlreadyExists,
    MovementConflictUnresolved,
    MovementCurrencyInvalid,
    MovementInvalid,
    StatementInvalid,
    TransferInvalid,
    UserAlreadyExists,
    UserInvalid,
    UserNotFound,
}
