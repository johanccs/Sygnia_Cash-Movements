using Grpc.Core;
using MediatR;
using Sygnia.Application.Commands.CreateAccount;
using Sygnia.Application.Queries.GetAccount;
using Sygnia.Application.Queries.ListAccounts;
using Sygnia.Presentation.Mapping;

namespace Sygnia.Presentation.Services;

/// <summary>
/// Thin transport layer: maps wire messages to MediatR commands/queries and back, and turns a
/// failed <see cref="Sygnia.Domain.Result{T}"/> into an <see cref="RpcException"/> via
/// <see cref="ResultExtensions"/>. No business logic lives here — that's Sygnia.Application's.
/// </summary>
internal sealed class AccountGrpcService(IMediator mediator) : AccountService.AccountServiceBase
{
    public override async Task<Account> CreateAccount(CreateAccountRequest request, ServerCallContext context)
    {
        var command = new CreateAccountCommand(
            request.AccountId,
            request.AccountName,
            string.IsNullOrEmpty(request.ContactPerson) ? null : request.ContactPerson,
            request.Currency,
            request.CreatedBy);

        var result = await mediator.Send(command, context.CancellationToken);
        return result.IsSuccess ? result.Value.ToProto() : throw result.Error.ToRpcException();
    }

    public override async Task<Account> GetAccount(GetAccountRequest request, ServerCallContext context)
    {
        var result = await mediator.Send(new GetAccountQuery(request.AccountId), context.CancellationToken);
        return result.IsSuccess ? result.Value.ToProto() : throw result.Error.ToRpcException();
    }

    public override async Task<ListAccountsResponse> ListAccounts(ListAccountsRequest request, ServerCallContext context)
    {
        var accounts = await mediator.Send(new ListAccountsQuery(), context.CancellationToken);
        var response = new ListAccountsResponse();
        response.Accounts.AddRange(accounts.Select(a => a.ToProto()));
        return response;
    }
}
