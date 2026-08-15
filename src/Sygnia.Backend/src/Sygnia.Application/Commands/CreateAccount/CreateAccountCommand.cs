using MediatR;
using Sygnia.Domain;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Commands.CreateAccount;

public sealed record CreateAccountCommand(
    string AccountId,
    string AccountName,
    string? ContactPerson,
    string Currency,
    string CreatedBy) : IRequest<Result<Account>>;
