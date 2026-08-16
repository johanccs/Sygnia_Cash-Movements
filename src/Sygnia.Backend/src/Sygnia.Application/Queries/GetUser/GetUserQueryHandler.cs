using MediatR;
using Sygnia.Application.Interfaces;
using Sygnia.Domain;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Queries.GetUser;

internal sealed class GetUserQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetUserQuery, Result<User>>
{
    public async Task<Result<User>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetAsync(request.Id, cancellationToken);
        return user is not null
            ? Result<User>.Success(user)
            : Result<User>.Failure(new Error(ErrorCode.UserNotFound, $"User '{request.Id}' was not found."));
    }
}
