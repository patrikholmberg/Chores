using Chores.Application.Common.Exceptions;
using Chores.Application.Common.Persistence;
using Chores.Domain.Families;

namespace Chores.Application.Features.Members;

/// <summary>A Member as Parents see it when managing the Family. Never carries password data.</summary>
public sealed record MemberDto(Guid Id, string Username, string DisplayName, Role Role, string PreferredLanguage, string? Email)
{
    public static MemberDto From(Member member) => new(
        member.Id,
        member.Username.Value,
        member.DisplayName,
        member.Role,
        member.PreferredLanguage.Code,
        member.Email);
}

internal static class FamilyLookup
{
    /// <summary>The current Family; a Parent-only request always has one.</summary>
    public static async Task<Family> GetRequiredAsync(this IFamilyRepository families, CancellationToken cancellationToken) =>
        await families.GetCurrentAsync(cancellationToken) ?? throw new ForbiddenException("No Family is selected.");

    /// <summary>A current Member of the Family; Members of other Families are reported as not found.</summary>
    public static Member GetMember(this Family family, Guid memberId) =>
        family.Members.SingleOrDefault(m => m.Id == memberId) ?? throw new NotFoundException("Member", memberId);
}
