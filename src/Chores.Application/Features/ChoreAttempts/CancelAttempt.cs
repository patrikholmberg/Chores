using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Mediator;

namespace Chores.Application.Features.ChoreAttempts;

/// <summary>
/// Removes an open attempt, for example one started by mistake. The owning Child or any Parent may cancel it; it never
/// earns points.
/// </summary>
public sealed record CancelAttemptCommand(Guid AttemptId) : ICommand;

public sealed class CancelAttemptHandler(
    IChoreAttemptRepository attempts,
    ICurrentMember currentMember,
    IUnitOfWork unitOfWork,
    TimeProvider time) : ICommandHandler<CancelAttemptCommand>
{
    public async ValueTask<Unit> Handle(CancelAttemptCommand command, CancellationToken cancellationToken)
    {
        var attempt = await attempts.LoadVisibleAsync(command.AttemptId, currentMember, cancellationToken);

        attempt.EnsureCancellable(time.GetUtcNow());
        attempts.Remove(attempt);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
