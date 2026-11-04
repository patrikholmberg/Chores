using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Application.Features.Members;
using Mediator;

namespace Chores.Application.Features.ChoreAttempts;

/// <summary>The Child sends an open attempt back to its first Step. It keeps its start day and expiry time.</summary>
public sealed record RestartAttemptCommand(Guid AttemptId) : ICommand<AttemptDto>, IChildOnly;

public sealed class RestartAttemptHandler(
    IChoreAttemptRepository attempts,
    IFamilyRepository families,
    ICurrentMember currentMember,
    IUnitOfWork unitOfWork,
    TimeProvider time) : ICommandHandler<RestartAttemptCommand, AttemptDto>
{
    public async ValueTask<AttemptDto> Handle(RestartAttemptCommand command, CancellationToken cancellationToken)
    {
        var attempt = await attempts.LoadVisibleAsync(command.AttemptId, currentMember, cancellationToken);
        var now = time.GetUtcNow();

        attempt.Restart(now);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return AttemptDto.From(attempt, new MemberNames(await families.GetRequiredAsync(cancellationToken)), now);
    }
}
