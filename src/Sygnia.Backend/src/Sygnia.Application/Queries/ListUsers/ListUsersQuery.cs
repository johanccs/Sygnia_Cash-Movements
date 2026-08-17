using MediatR;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Queries.ListUsers;

public sealed record ListUsersQuery : IRequest<IReadOnlyList<User>>;
