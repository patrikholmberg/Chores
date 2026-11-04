using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Application.Features.Members;
using Mediator;

namespace Chores.Application.Features.ChoreAttempts;

/// <summary>
/// The Child completes the Step at <see cref="Position"/>, which must be the current one, so a repeated request cannot
/// complete two Steps.
/// </summary>
public sealed record CompleteStepCommand(Guid AttemptId, int Position) : ICommand<AttemptDto>, IChildOnly;

public sealed class CompleteStepHandler(
    IChoreAttemptRepository attempts,
    IFamilyRepository families,
    ICurrentMember currentMember,
    IUnitOfWork unitOfWork,
    TimeProvider time) : ICommandHandler<CompleteStepCommand, AttemptDto>
{
    public async ValueTask<AttemptDto> Handle(CompleteStepCommand command, CancellationToken cancellationToken)
    {
        var attempt = await attempts.LoadVisibleAsync(command.AttemptId, currentMember, cancellationToken);
        var now = time.GetUtcNow();

        attempt.CompleteStep(command.Position, now);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return AttemptDto.From(attempt, new MemberNames(await families.GetRequiredAsync(cancellationToken)), now);
    }
}
