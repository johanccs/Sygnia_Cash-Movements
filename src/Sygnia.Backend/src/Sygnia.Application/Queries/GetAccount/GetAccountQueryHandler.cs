using MediatR;
using Sygnia.Application.Interfaces;
using Sygnia.Domain;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Queries.GetAccount;

internal sealed class GetAccountQueryHandler(IAccountRepository accountRepository)
    : IRequestHandler<GetAccountQuery, Result<Account>>
{
    public async Task<Result<Account>> Handle(GetAccountQuery request, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetAsync(request.AccountId, cancellationToken);
        return account is not null
            ? Result<Account>.Success(account)
            : Result<Account>.Failure(new Error(ErrorCode.AccountNotFound, $"Account '{request.AccountId}' was not found."));
    }
}
