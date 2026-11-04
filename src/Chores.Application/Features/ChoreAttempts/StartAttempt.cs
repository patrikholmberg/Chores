using Chores.Application.Common.Exceptions;
using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Application.Features.Members;
using Chores.Domain.ChoreAttempts;
using Mediator;

namespace Chores.Application.Features.ChoreAttempts;

/// <summary>A Child starts a Chore Attempt from a Chore in the library. Archived Chores do not exist to a Child.</summary>
public sealed record StartAttemptCommand(Guid ChoreId) : ICommand<AttemptDto>, IChildOnly;

public sealed class StartAttemptHandler(
    IChoreRepository chores,
    IChoreAttemptRepository attempts,
    IFamilyRepository families,
    ICurrentMember currentMember,
    IUnitOfWork unitOfWork,
    TimeProvider time) : ICommandHandler<StartAttemptCommand, AttemptDto>
{
    public async ValueTask<AttemptDto> Handle(StartAttemptCommand command, CancellationToken cancellationToken)
    {
        var childId = currentMember.MemberId ?? throw new ForbiddenException("Only a signed-in Child can start a Chore.");
        var chore = await chores.GetAsync(command.ChoreId, cancellationToken);
        if (chore is null || chore.IsArchived)
        {
            throw new NotFoundException("Chore", command.ChoreId);
        }

        var family = await families.GetRequiredAsync(cancellationToken);
        var now = time.GetUtcNow();
        var attempt = ChoreAttempt.Start(Guid.CreateVersion7(), childId, chore, family.TimeZone, family.AttemptWindow, now);

        attempts.Add(attempt);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return AttemptDto.From(attempt, new MemberNames(family), now);
    }
}
