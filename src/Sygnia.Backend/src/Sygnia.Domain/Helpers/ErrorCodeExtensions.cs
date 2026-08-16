namespace Sygnia.Domain;

/// <summary>The one place an <see cref="ErrorCode"/> is converted to its wire string.</summary>
public static class ErrorCodeExtensions
{
    public static string ToCode(this ErrorCode code) => code switch
    {
        ErrorCode.AccountAlreadyExists => "account.already_exists",
        ErrorCode.AccountContactPersonInvalid => "account.contactperson.invalid",
        ErrorCode.AccountInvalid => "account.invalid",
        ErrorCode.AccountNameInvalid => "account.name.invalid",
        ErrorCode.AccountNotFound => "account.not_found",
        ErrorCode.BalanceInvalid => "balance.invalid",
        ErrorCode.MovementAlreadyExists => "movement.already_exists",
        ErrorCode.MovementConflictUnresolved => "movement.conflict_unresolved",
        ErrorCode.MovementCurrencyInvalid => "movement.currency.invalid",
        ErrorCode.MovementInvalid => "movement.invalid",
        ErrorCode.StatementInvalid => "statement.invalid",
        ErrorCode.TransferInvalid => "transfer.invalid",
        ErrorCode.UserAlreadyExists => "user.already_exists",
        ErrorCode.UserInvalid => "user.invalid",
        ErrorCode.UserNotFound => "user.not_found",
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown ErrorCode; add it to ToCode()."),
    };
}
