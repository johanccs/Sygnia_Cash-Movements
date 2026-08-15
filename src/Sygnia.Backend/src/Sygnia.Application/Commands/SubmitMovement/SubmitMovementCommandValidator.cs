using FluentValidation;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Commands.SubmitMovement;

/// <summary>
/// Catches malformed input before it reaches <see cref="Movement"/>'s constructor, so a bad
/// request comes back as a <c>Result</c> failure (INVALID_ARGUMENT) rather than an exception.
/// Mirrors the field constraints on <see cref="Movement"/> rather than retyping them.
/// </summary>
internal sealed class SubmitMovementCommandValidator : AbstractValidator<SubmitMovementCommand>
{
    public SubmitMovementCommandValidator()
    {
        RuleFor(c => c.AccountId)
            .NotEmpty()
            .MaximumLength(Movement.AccountIdMaxLength);

        RuleFor(c => c.ExternalRef)
            .NotEmpty()
            .MaximumLength(Movement.ExternalRefMaxLength);

        RuleFor(c => c.Currency)
            .NotEmpty()
            .Length(3);

        RuleFor(c => c.Amount)
            .NotEqual(0m);

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
