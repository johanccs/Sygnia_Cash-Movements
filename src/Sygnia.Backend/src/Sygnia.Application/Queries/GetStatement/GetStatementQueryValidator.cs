using FluentValidation;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Queries.GetStatement;

internal sealed class GetStatementQueryValidator : AbstractValidator<GetStatementQuery>
{
    public GetStatementQueryValidator()
    {
        RuleFor(q => q.AccountId)
            .NotEmpty()
            .MaximumLength(Movement.AccountIdMaxLength);

        RuleFor(q => q.From)
            .LessThanOrEqualTo(q => q.To)
            .WithMessage("'From' must not be after 'To'.");
    }
}
