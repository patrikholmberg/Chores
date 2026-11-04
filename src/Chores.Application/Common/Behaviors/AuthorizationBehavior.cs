using Chores.Application.Common.Exceptions;
using Chores.Application.Common.Tenancy;
using Chores.Domain.Families;
using Mediator;

namespace Chores.Application.Common.Behaviors;

/// <summary>
/// Refuses <see cref="IParentOnly"/> messages from anyone but a Parent and <see cref="IChildOnly"/> messages from anyone
/// but a Child, before validation or the handler run, so the rules hold whichever host sends the message.
/// </summary>
public sealed class AuthorizationBehavior<TMessage, TResponse>(ICurrentMember currentMember)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (message is IParentOnly && currentMember.Role != Role.Parent)
        {
            throw new ForbiddenException("Only a Parent can do this.");
        }

        if (message is IChildOnly && currentMember.Role != Role.Child)
        {
            throw new ForbiddenException("Only a Child can do this.");
        }

        return next(message, cancellationToken);
    }
}
