using MediatR;
using Sygnia.Domain;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Commands.SubmitMovement;

public sealed record SubmitMovementCommand(
    string AccountId,
    string ExternalRef,
    string Currency,
    decimal Amount,
    DateTime OccurredAt,
    string? Narration,
    Guid RefNr,
    string MovedBy,
    DateTime MovedDate) : IRequest<Result<Movement>>;
