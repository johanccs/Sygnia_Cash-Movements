using FluentValidation;
using FluentValidation.Results;
using Sygnia.Application.Behaviours;
using Sygnia.Domain;

namespace Sygnia.Tests.Application;

public sealed class ValidationBehaviourTests
{
    private sealed record ValidatedPing(string Value) : MediatR.IRequest<Result<string>>, IValidatedRequest
    {
        public ErrorCode ValidationErrorCode => ErrorCode.MovementInvalid;
    }

    private sealed record UnvalidatedPing : MediatR.IRequest<string>;

    private sealed class AlwaysInvalidValidator : AbstractValidator<ValidatedPing>
    {
        public AlwaysInvalidValidator() => RuleFor(x => x.Value).Empty().WithMessage("must be empty");
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsNext()
    {
        var behaviour = new ValidationBehaviour<ValidatedPing, Result<string>>([]);

        var result = await behaviour.Handle(
            new ValidatedPing("x"), () => Task.FromResult(Result<string>.Success("ok")), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("ok", result.Value);
    }

    [Fact]
    public async Task Handle_FailingValidator_ShortCircuitsWithMappedErrorCode()
    {
        var behaviour = new ValidationBehaviour<ValidatedPing, Result<string>>([new AlwaysInvalidValidator()]);
        var nextCalled = false;

        var result = await behaviour.Handle(
            new ValidatedPing("not empty"),
            () => { nextCalled = true; return Task.FromResult(Result<string>.Success("unreachable")); },
            CancellationToken.None);

        Assert.False(nextCalled);
        Assert.True(result.IsFailure);
        Assert.Equal("movement.invalid", result.Error.Code);
        Assert.Contains("must be empty", result.Error.Message);
    }

    [Fact]
    public async Task Handle_RequestNotIValidatedRequest_PassesThroughUnchanged()
    {
        var behaviour = new ValidationBehaviour<UnvalidatedPing, string>([]);

        var result = await behaviour.Handle(new UnvalidatedPing(), () => Task.FromResult("pong"), CancellationToken.None);

        Assert.Equal("pong", result);
    }
}
