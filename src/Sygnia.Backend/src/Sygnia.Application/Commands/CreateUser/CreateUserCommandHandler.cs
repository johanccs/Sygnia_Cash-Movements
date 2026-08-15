using FluentValidation;
using MediatR;
using Sygnia.Application.Interfaces;
using Sygnia.Domain;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Commands.CreateUser;

internal sealed class CreateUserCommandHandler(
    IUserRepository userRepository,
    IValidator<CreateUserCommand> validator) : IRequestHandler<CreateUserCommand, Result<User>>
{
    public async Task<Result<User>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var message = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<User>.Failure(new Error("user.invalid", message));
        }

        var user = new User(request.Id, request.Name, request.Surname);

        return await userRepository.CreateAsync(user, cancellationToken);
    }
}
