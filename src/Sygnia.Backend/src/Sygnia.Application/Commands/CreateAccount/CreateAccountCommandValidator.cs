using FluentValidation;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Commands.CreateAccount;

/// <summary>Mirrors the field constraints on <see cref="Account"/> rather than retyping them.</summary>
internal sealed class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(c => c.AccountId)
            .NotEmpty()
            .MaximumLength(Account.AccountIdMaxLength);

        RuleFor(c => c.AccountName)
            .NotEmpty()
            .MaximumLength(Account.AccountNameMaxLength);

        RuleFor(c => c.ContactPerson)
            .MaximumLength(Account.ContactPersonMaxLength);

        RuleFor(c => c.Currency)
            .NotEmpty()
            .Length(3);

        RuleFor(c => c.CreatedBy)
            .NotEmpty()
            .MaximumLength(Account.CreatedByMaxLength);
    }
}
