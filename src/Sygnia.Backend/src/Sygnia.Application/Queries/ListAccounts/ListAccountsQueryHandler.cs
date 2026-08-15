using MediatR;
using Sygnia.Application.Interfaces;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Queries.ListAccounts;

internal sealed class ListAccountsQueryHandler(IAccountRepository accountRepository)
    : IRequestHandler<ListAccountsQuery, IReadOnlyList<Account>>
{
    public Task<IReadOnlyList<Account>> Handle(ListAccountsQuery request, CancellationToken cancellationToken) =>
        accountRepository.ListAsync(cancellationToken);
}
