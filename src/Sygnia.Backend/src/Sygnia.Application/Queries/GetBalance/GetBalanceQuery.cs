using MediatR;
using Sygnia.Domain;

namespace Sygnia.Application.Queries.GetBalance;

public sealed record GetBalanceQuery(string AccountId) : IRequest<Result<decimal>>;
