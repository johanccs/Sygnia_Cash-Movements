using FluentValidation;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Commands.CreateUser;

internal sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(c => c.Id)
            .NotEmpty()
            .MaximumLength(User.IdMaxLength);

        RuleFor(c => c.Name)
            .NotEmpty()
            .MaximumLength(User.NameMaxLength);

        RuleFor(c => c.Surname)
            .NotEmpty()
            .MaximumLength(User.SurnameMaxLength);
    }
}
