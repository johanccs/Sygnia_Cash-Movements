using MediatR;
using Sygnia.Application.Interfaces;
using Sygnia.Domain;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Commands.CreateUser;

internal sealed class CreateUserCommandHandler(IUserRepository userRepository)
    : IRequestHandler<CreateUserCommand, Result<User>>
{
    // Validation runs in ValidationBehaviour<,> before this handler is reached.
    public async Task<Result<User>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User(request.Id, request.Name, request.Surname);

        return await userRepository.CreateAsync(user, cancellationToken);
    }
}
