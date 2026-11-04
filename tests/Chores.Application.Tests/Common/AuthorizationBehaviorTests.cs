using Chores.Application.Common.Behaviors;
using Chores.Application.Common.Exceptions;
using Chores.Application.Common.Tenancy;
using Chores.Domain.Families;
using Mediator;

namespace Chores.Application.Tests.Common;

public class AuthorizationBehaviorTests
{
    private sealed record ParentCommand : ICommand<string>, IParentOnly;

    private sealed record AnyoneQuery : IQuery<string>;

    private static async Task<(bool Invoked, Exception? Error)> SendAsync<TMessage>(TMessage message, Role? role)
        where TMessage : IMessage
    {
        var invoked = false;
        var behavior = new AuthorizationBehavior<TMessage, string>(new FakeCurrentMember(role));
        var error = await Record.ExceptionAsync(async () => await behavior.Handle(
            message,
            (_, _) => { invoked = true; return ValueTask.FromResult("ok"); },
            TestContext.Current.CancellationToken));
        return (invoked, error);
    }

    [Theory]
    [InlineData(Role.Child)]
    [InlineData(null)]
    public async Task Non_parents_are_refused_before_the_handler_runs(Role? role)
    {
        var (invoked, error) = await SendAsync(new ParentCommand(), role);

        Assert.IsType<ForbiddenException>(error);
        Assert.False(invoked);
    }

    [Fact]
    public async Task A_parent_reaches_the_handler()
    {
        var (invoked, error) = await SendAsync(new ParentCommand(), Role.Parent);

        Assert.Null(error);
        Assert.True(invoked);
    }

    [Fact]
    public async Task Messages_without_the_marker_are_not_restricted()
    {
        var (invoked, _) = await SendAsync(new AnyoneQuery(), Role.Child);

        Assert.True(invoked);
    }
}
