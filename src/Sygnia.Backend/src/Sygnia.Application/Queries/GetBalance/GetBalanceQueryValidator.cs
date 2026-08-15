using FluentValidation;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Queries.GetBalance;

internal sealed class GetBalanceQueryValidator : AbstractValidator<GetBalanceQuery>
{
    public GetBalanceQueryValidator()
    {
        RuleFor(q => q.AccountId)
            .NotEmpty()
            .MaximumLength(Movement.AccountIdMaxLength);
    }
}
