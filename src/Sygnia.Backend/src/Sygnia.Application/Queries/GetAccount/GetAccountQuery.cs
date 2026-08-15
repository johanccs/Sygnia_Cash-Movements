using MediatR;
using Sygnia.Domain;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Queries.GetAccount;

public sealed record GetAccountQuery(string AccountId) : IRequest<Result<Account>>;
