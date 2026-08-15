using FluentValidation;
using MediatR;
using Sygnia.Application.Interfaces;
using Sygnia.Domain;

namespace Sygnia.Application.Queries.GetStatement;

/// <summary>
/// Yields rows while accumulating a running total in one <c>decimal</c> — never buffers the
/// statement into a list first. See <see cref="IStatementReader"/> for the streaming contract
/// this depends on.
/// </summary>
internal sealed class GetStatementQueryHandler(
    IStatementReader statementReader,
    IValidator<GetStatementQuery> validator) : IStreamRequestHandler<GetStatementQuery, StatementLine>
{
    public async IAsyncEnumerable<StatementLine> Handle(
        GetStatementQuery request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var message = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
            yield return StatementLine.WithError(new Error("statement.invalid", message));
            yield break;
        }

        if (!await statementReader.AccountExistsAsync(request.AccountId, cancellationToken))
        {
            yield return StatementLine.WithError(
                new Error("account.not_found", $"Account '{request.AccountId}' does not exist."));
            yield break;
        }

        var runningTotal = 0m;
        await foreach (var movement in statementReader
            .StreamAsync(request.AccountId, request.From, request.To, cancellationToken)
            .WithCancellation(cancellationToken))
        {
            runningTotal += movement.Amount;
            yield return StatementLine.Of(movement, runningTotal);
        }
    }
}
