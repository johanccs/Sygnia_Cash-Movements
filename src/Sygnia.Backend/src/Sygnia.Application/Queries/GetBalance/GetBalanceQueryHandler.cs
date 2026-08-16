using FluentValidation;
using MediatR;
using Sygnia.Application.Interfaces;
using Sygnia.Domain;

namespace Sygnia.Application.Queries.GetBalance;

internal sealed class GetBalanceQueryHandler(
    IBalanceReader balanceReader,
    IValidator<GetBalanceQuery> validator) : IRequestHandler<GetBalanceQuery, Result<decimal>>
{
    public async Task<Result<decimal>> Handle(GetBalanceQuery request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var message = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<decimal>.Failure(new Error(ErrorCode.BalanceInvalid, message));
        }

        return await balanceReader.GetBalanceAsync(request.AccountId, cancellationToken);
    }
}
