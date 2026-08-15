using FluentValidation;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Queries.GetStatementPage;

internal sealed class GetStatementPageQueryValidator : AbstractValidator<GetStatementPageQuery>
{
    public GetStatementPageQueryValidator()
    {
        RuleFor(q => q.AccountId)
            .NotEmpty()
            .MaximumLength(Movement.AccountIdMaxLength);

        RuleFor(q => q.From)
            .LessThanOrEqualTo(q => q.To)
            .WithMessage("'From' must not be after 'To'.");

        RuleFor(q => q.PageNumber)
            .GreaterThanOrEqualTo(1);

        RuleFor(q => q.PageSize)
            .GreaterThanOrEqualTo(1);
    }
}
