using MediatR;
using Sygnia.Application.Behaviours;
using Sygnia.Domain;

namespace Sygnia.Application.Commands.TransferFunds;

/// <summary>
/// One atomic transfer, written as two <c>Movement</c> legs in a single transaction.
/// <see cref="ExternalRef"/> is the caller's idempotency key for the transfer as a whole; the
/// handler derives the two legs' own (per-account) external refs from it, so replaying the
/// same <see cref="ExternalRef"/> replays both legs identically. Leave room within the
/// 20-character <c>ExternalRef</c> limit for the "-DR"/"-CR" suffix.
/// </summary>
public sealed record TransferFundsCommand(
    string FromAccountId,
    string ToAccountId,
    string ExternalRef,
    string Currency,
    decimal Amount,
    DateTime OccurredAt,
    string? Narration,
    Guid RefNr,
    string MovedBy,
    DateTime MovedDate) : IRequest<Result<TransferResult>>, IValidatedRequest
{
    public ErrorCode ValidationErrorCode => ErrorCode.TransferInvalid;
}

/// <summary>The two legs written atomically by a <see cref="TransferFundsCommand"/>.</summary>
public sealed record TransferResult(Domain.Models.Movement Debit, Domain.Models.Movement Credit);
