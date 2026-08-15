using MediatR;
using Sygnia.Domain;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Queries.GetStatementPage;

public sealed record GetStatementPageQuery(
    string AccountId, DateTime From, DateTime To, int PageNumber, int PageSize) : IRequest<Result<StatementPage>>;

/// <summary>
/// One buffered page for the UI's paginated table. Unlike <see cref="StatementLine"/>'s
/// streaming counterpart, this is a normal request/response value — a single page is small by
/// construction, so buffering it does not touch the streaming invariant.
/// </summary>
public sealed record StatementPage(IReadOnlyList<Movement> Rows, int TotalCount);
