using FluentValidation;
using MediatR;
using Sygnia.Application.Interfaces;
using Sygnia.Domain;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Commands.CreateAccount;

internal sealed class CreateAccountCommandHandler(
    IAccountRepository accountRepository,
    IValidator<CreateAccountCommand> validator) : IRequestHandler<CreateAccountCommand, Result<Account>>
{
    public async Task<Result<Account>> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var message = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<Account>.Failure(new Error("account.invalid", message));
        }

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
