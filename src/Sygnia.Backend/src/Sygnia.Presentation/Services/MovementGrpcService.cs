using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Sygnia.Application.Commands.SubmitMovement;
using Sygnia.Application.Commands.TransferFunds;
using Sygnia.Application.Queries.GetBalance;
using Sygnia.Application.Queries.GetStatement;
using Sygnia.Application.Queries.GetStatementPage;
using Sygnia.Presentation.Mapping;

namespace Sygnia.Presentation.Services;

/// <summary>
/// Thin transport layer: maps wire messages to MediatR commands/queries and back, and turns a
/// failed <see cref="Sygnia.Domain.Result{T}"/> into an <see cref="RpcException"/> via
/// <see cref="ResultExtensions"/>. No business logic lives here — that's Sygnia.Application's.
/// </summary>
internal sealed class MovementGrpcService(IMediator mediator) : MovementService.MovementServiceBase
{
    public override async Task<Movement> SubmitMovement(SubmitMovementRequest request, ServerCallContext context)
    {
        var command = new SubmitMovementCommand(
            request.AccountId,
            request.ExternalRef,
            request.Currency,
            request.Amount.ToDecimalAmount(),
            request.OccurredAt.ToDateTime(),
            request.Narration,
            Guid.Parse(request.RefNr),
            request.MovedBy,
            request.MovedDate.ToDateTime());

        var result = await mediator.Send(command, context.CancellationToken);
        return result.IsSuccess ? result.Value.ToProto() : throw result.Error.ToRpcException();
    }

    public override async Task<TransferResponse> Transfer(TransferRequest request, ServerCallContext context)
    {
        var command = new TransferFundsCommand(
            request.FromAccountId,
            request.ToAccountId,
            request.ExternalRef,
            request.Currency,
            request.Amount.ToDecimalAmount(),
            request.OccurredAt.ToDateTime(),
            request.Narration,
            Guid.Parse(request.RefNr),
            request.MovedBy,
            request.MovedDate.ToDateTime());

        var result = await mediator.Send(command, context.CancellationToken);
        if (result.IsFailure)
        {
            throw result.Error.ToRpcException();
        }

        return new TransferResponse
        {
            Debit = result.Value.Debit.ToProto(),
            Credit = result.Value.Credit.ToProto(),
        };
    }

    public override async Task<GetBalanceResponse> GetBalance(GetBalanceRequest request, ServerCallContext context)
    {
        var result = await mediator.Send(new GetBalanceQuery(request.AccountId), context.CancellationToken);
        if (result.IsFailure)
        {
            throw result.Error.ToRpcException();
        }

        return new GetBalanceResponse
        {
            AccountId = request.AccountId,
            Balance = result.Value.ToAmountString(),
        };
    }

    public override async Task GetStatement(
        GetStatementRequest request,
        IServerStreamWriter<StatementLine> responseStream,
        ServerCallContext context)
    {
        var query = new GetStatementQuery(request.AccountId, request.From.ToDateTime(), request.To.ToDateTime());

        // Streams end to end — never buffers into a list before writing. Each line is written
        // to the wire as it's produced by the handler.
        await foreach (var line in mediator.CreateStream(query, context.CancellationToken))
        {
            if (line.Error is not null)
            {
                throw line.Error.ToRpcException();
            }

            await responseStream.WriteAsync(new StatementLine
            {
                Movement = line.Movement!.ToProto(),
                RunningTotal = line.RunningTotal.ToAmountString(),
            });
        }
    }

    /// <summary>
    /// A buffered page for the UI's paginated table — a normal request/response RPC, distinct
    /// from <see cref="GetStatement"/>'s server streaming. Does not compute a running total,
    /// since that is only meaningful over the full ordered stream.
    /// </summary>
    public override async Task<GetStatementPageResponse> GetStatementPage(
        GetStatementPageRequest request, ServerCallContext context)
    {
        var query = new GetStatementPageQuery(
            request.AccountId,
            request.From.ToDateTime(),
            request.To.ToDateTime(),
            request.PageNumber,
            request.PageSize);

        var result = await mediator.Send(query, context.CancellationToken);
        if (result.IsFailure)
        {
            throw result.Error.ToRpcException();
        }

        var response = new GetStatementPageResponse { TotalCount = result.Value.TotalCount };
        response.Lines.AddRange(result.Value.Rows.Select(movement => new StatementLine
        {
            Movement = movement.ToProto(),
        }));

        return response;
    }
}
