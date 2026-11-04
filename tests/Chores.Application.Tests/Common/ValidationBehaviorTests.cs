using Chores.Application.Common.Behaviors;
using FluentValidation;
using Mediator;

namespace Chores.Application.Tests.Common;

public class ValidationBehaviorTests
{
    private sealed record Ping(string Name) : IQuery<string>;

    private sealed class PingValidator : AbstractValidator<Ping>
    {
        public PingValidator() => RuleFor(p => p.Name).NotEmpty();
    }

    [Fact]
    public async Task Invalid_message_throws_and_handler_is_not_invoked()
    {
        var behavior = new ValidationBehavior<Ping, string>([new PingValidator()]);
        var handlerInvoked = false;

        await Assert.ThrowsAsync<ValidationException>(async () =>
            await behavior.Handle(
                new Ping(""),
                (_, _) => { handlerInvoked = true; return ValueTask.FromResult("pong"); },
                TestContext.Current.CancellationToken));

        Assert.False(handlerInvoked);
    }

    [Fact]
    public async Task Valid_message_reaches_handler()
    {
        var behavior = new ValidationBehavior<Ping, string>([new PingValidator()]);

        var result = await behavior.Handle(
            new Ping("Emma"),
            (_, _) => ValueTask.FromResult("pong"),
            TestContext.Current.CancellationToken);

        Assert.Equal("pong", result);
    }
}
