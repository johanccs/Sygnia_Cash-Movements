using MediatR;
using Sygnia.Application.Interfaces;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Queries.ListUsers;

internal sealed class ListUsersQueryHandler(IUserRepository userRepository)
    : IRequestHandler<ListUsersQuery, IReadOnlyList<User>>
{
    public Task<IReadOnlyList<User>> Handle(ListUsersQuery request, CancellationToken cancellationToken) =>
        userRepository.ListAsync(cancellationToken);
}
