using MediatR;
using Sygnia.Domain;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Commands.CreateUser;

public sealed record CreateUserCommand(string Id, string Name, string Surname) : IRequest<Result<User>>;
