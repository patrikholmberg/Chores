using Chores.Application.Common.Identity;
using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Mediator;

namespace Chores.Application.Features.Members;

/// <summary>
/// Removes a Member: they are kept for history but can no longer sign in, their username becomes free, and their
/// sessions end. The last Parent cannot be removed.
/// </summary>
public sealed record RemoveMemberCommand(Guid MemberId) : ICommand, IParentOnly;

public sealed class RemoveMemberHandler(
    IFamilyRepository families,
    IMemberCredentials credentials,
    IUnitOfWork unitOfWork,
    TimeProvider time) : ICommandHandler<RemoveMemberCommand>
{
    public async ValueTask<Unit> Handle(RemoveMemberCommand command, CancellationToken cancellationToken)
    {
        var family = await families.GetRequiredAsync(cancellationToken);
        var member = family.GetMember(command.MemberId);

        family.RemoveMember(member.Id, time.GetUtcNow());
        await credentials.DeleteAsync(member.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await credentials.RevokeSessionsAsync(member.Id, cancellationToken);

        return Unit.Value;
    }
}
