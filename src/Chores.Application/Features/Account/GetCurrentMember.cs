using Chores.Application.Common.Exceptions;
using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Domain.Families;
using Mediator;

namespace Chores.Application.Features.Account;

/// <summary>The signed-in Member's own profile and their Family.</summary>
public sealed record GetCurrentMemberQuery : IQuery<CurrentMemberProfile>;

public sealed record CurrentMemberProfile(
    Guid MemberId,
    string Username,
    string DisplayName,
    Role Role,
    string PreferredLanguage,
    Guid FamilyId,
    string FamilyName,
    string TimeZone);

public sealed class GetCurrentMemberHandler(ICurrentMember currentMember, IFamilyRepository families)
    : IQueryHandler<GetCurrentMemberQuery, CurrentMemberProfile>
{
    public async ValueTask<CurrentMemberProfile> Handle(GetCurrentMemberQuery query, CancellationToken cancellationToken)
    {
        var family = await families.GetCurrentAsync(cancellationToken);
        var member = family?.Members.SingleOrDefault(m => m.Id == currentMember.MemberId)
            ?? throw new NotFoundException("Member", currentMember.MemberId?.ToString() ?? "(none)");

        return new CurrentMemberProfile(
            member.Id,
            member.Username.Value,
            member.DisplayName,
            member.Role,
            member.PreferredLanguage.Code,
            family.Id,
            family.Name,
            family.TimeZone.Id);
    }
}
