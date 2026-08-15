using MediatR;
using Sygnia.Domain;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Queries.GetUser;

public sealed record GetUserQuery(string Id) : IRequest<Result<User>>;
