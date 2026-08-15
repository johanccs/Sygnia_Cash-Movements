using FluentValidation;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Commands.TransferFunds;

internal sealed class TransferFundsCommandValidator : AbstractValidator<TransferFundsCommand>
{
    private const int LegSuffixLength = 3; // "-DR" / "-CR"

    public TransferFundsCommandValidator()
    {
        RuleFor(c => c.FromAccountId)
            .NotEmpty()
            .MaximumLength(Movement.AccountIdMaxLength);

        RuleFor(c => c.ToAccountId)
            .NotEmpty()
            .MaximumLength(Movement.AccountIdMaxLength);

        RuleFor(c => c)
            .Must(c => !string.Equals(c.FromAccountId, c.ToAccountId, StringComparison.Ordinal))
            .WithMessage("'From Account Id' and 'To Account Id' must differ.")
            .When(c => !string.IsNullOrWhiteSpace(c.FromAccountId) && !string.IsNullOrWhiteSpace(c.ToAccountId));

        RuleFor(c => c.ExternalRef)
            .NotEmpty()
            .MaximumLength(Movement.ExternalRefMaxLength - LegSuffixLength);

        RuleFor(c => c.Currency)
            .NotEmpty()
            .Length(3);

        RuleFor(c => c.Amount)
            .GreaterThan(0m)
            .WithMessage("'Amount' must be positive; the handler applies the sign per leg.");

        RuleFor(c => c.OccurredAt)
            .Must(d => d.Kind == DateTimeKind.Utc && d != default)
            .WithMessage("'Occurred At' must be a UTC timestamp.");

        RuleFor(c => c.Narration)
            .MaximumLength(Movement.NarrationMaxLength);

        RuleFor(c => c.RefNr)
            .NotEqual(Guid.Empty);

        RuleFor(c => c.MovedBy)
            .NotEmpty()
            .MaximumLength(Movement.MovedByMaxLength);

        RuleFor(c => c.MovedDate)
            .Must(d => d.Kind == DateTimeKind.Utc && d != default)
            .WithMessage("'Moved Date' must be a UTC timestamp.");
    }
}
