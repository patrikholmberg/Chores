using Chores.Application.Common.Tenancy;
using Chores.Domain.Families;

namespace Chores.Application.Tests.Common;

public sealed class FakeCurrentMember(Role? role = null, Guid? familyId = null, Guid? memberId = null) : ICurrentMember
{
    public Guid? MemberId { get; } = memberId;

    public Guid? FamilyId { get; } = familyId;

    public Role? Role { get; } = role;
}
