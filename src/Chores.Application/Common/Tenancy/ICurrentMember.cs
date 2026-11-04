using Chores.Domain.Families;

namespace Chores.Application.Common.Tenancy;

/// <summary>The Member a request runs as, taken from their access token. All values are null for anonymous callers.</summary>
public interface ICurrentMember
{
    Guid? MemberId { get; }

    Guid? FamilyId { get; }

    Role? Role { get; }
}
