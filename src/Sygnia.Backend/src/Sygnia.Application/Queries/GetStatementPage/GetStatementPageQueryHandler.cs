using FluentValidation;
using MediatR;
using Sygnia.Application.Interfaces;
using Sygnia.Domain;

namespace Sygnia.Application.Queries.GetStatementPage;

/// <summary>
/// A normal buffered request/response query for the UI's paginated table — distinct from
/// <see cref="Sygnia.Application.Queries.GetStatement.GetStatementQueryHandler"/>, which must
/// stream. See <see cref="IStatementReader.GetPageAsync"/> for why buffering a single page is
/// fine here.
/// </summary>
internal sealed class GetStatementPageQueryHandler(
    IStatementReader statementReader,
    IValidator<GetStatementPageQuery> validator) : IRequestHandler<GetStatementPageQuery, Result<StatementPage>>
{
    public async Task<Result<StatementPage>> Handle(GetStatementPageQuery request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var message = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<StatementPage>.Failure(new Error("statement.invalid", message));
        }

        if (!await statementReader.AccountExistsAsync(request.AccountId, cancellationToken))
        {
            return Result<StatementPage>.Failure(
                new Error("account.not_found", $"Account '{request.AccountId}' does not exist."));
        }

        var (rows, totalCount) = await statementReader.GetPageAsync(
            request.AccountId, request.From, request.To, request.PageNumber, request.PageSize, cancellationToken);

        return Result<StatementPage>.Success(new StatementPage(rows, totalCount));
    }
}
