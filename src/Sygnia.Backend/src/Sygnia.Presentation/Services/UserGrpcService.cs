using Grpc.Core;
using MediatR;
using Sygnia.Application.Commands.CreateUser;
using Sygnia.Application.Queries.GetUser;
using Sygnia.Presentation.Mapping;

namespace Sygnia.Presentation.Services;

/// <summary>
/// Thin transport layer: maps wire messages to MediatR commands/queries and back, and turns a
/// failed <see cref="Sygnia.Domain.Result{T}"/> into an <see cref="RpcException"/> via
/// <see cref="ResultExtensions"/>. No business logic lives here — that's Sygnia.Application's.
/// </summary>
internal sealed class UserGrpcService(IMediator mediator) : UserService.UserServiceBase
{
    public override async Task<User> CreateUser(CreateUserRequest request, ServerCallContext context)
    {
        var command = new CreateUserCommand(request.Id, request.Name, request.Surname);

        var result = await mediator.Send(command, context.CancellationToken);
        return result.IsSuccess ? result.Value.ToProto() : throw result.Error.ToRpcException();
    }

    public override async Task<User> GetUser(GetUserRequest request, ServerCallContext context)
    {
        var result = await mediator.Send(new GetUserQuery(request.Id), context.CancellationToken);
        return result.IsSuccess ? result.Value.ToProto() : throw result.Error.ToRpcException();
    }
}
