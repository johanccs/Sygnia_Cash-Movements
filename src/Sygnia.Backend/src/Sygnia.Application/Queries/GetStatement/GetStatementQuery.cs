using MediatR;
using Sygnia.Domain;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Queries.GetStatement;

public sealed record GetStatementQuery(string AccountId, DateTime From, DateTime To) : IStreamRequest<StatementLine>;

/// <summary>
/// One line of a streamed statement. <see cref="Error"/> is set only for the first (and only)
/// line when the request is invalid or the account does not exist — the stream ends there
/// rather than throwing, so an expected failure stays a value, not an exception, even across a
/// streaming boundary.
/// </summary>
public sealed record StatementLine(Movement? Movement, decimal RunningTotal, Error? Error)
{
    public static StatementLine Of(Movement movement, decimal runningTotal) => new(movement, runningTotal, null);

    public static StatementLine WithError(Error error) => new(null, 0m, error);
}
