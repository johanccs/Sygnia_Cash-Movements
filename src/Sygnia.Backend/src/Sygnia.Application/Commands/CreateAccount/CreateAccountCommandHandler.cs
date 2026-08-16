using MediatR;
using Sygnia.Application.Interfaces;
using Sygnia.Domain;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Commands.CreateAccount;

internal sealed class CreateAccountCommandHandler(IAccountRepository accountRepository)
    : IRequestHandler<CreateAccountCommand, Result<Account>>
{
    // Validation runs in ValidationBehaviour<,> before this handler is reached.
    public async Task<Result<Account>> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = new Account(
            request.AccountId,
            request.AccountName,
            request.ContactPerson,
            request.Currency,
            DateTime.UtcNow,
            request.CreatedBy);

        return await accountRepository.CreateAsync(account, cancellationToken);
    }
}
