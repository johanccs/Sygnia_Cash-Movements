using MediatR;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Queries.ListAccounts;

public sealed record ListAccountsQuery : IRequest<IReadOnlyList<Account>>;
