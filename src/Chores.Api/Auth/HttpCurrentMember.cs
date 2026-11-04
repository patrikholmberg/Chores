using Chores.Application.Common.Tenancy;
using Chores.Domain.Families;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Chores.Api.Auth;

/// <summary>The Member identified by the request's access token.</summary>
internal sealed class HttpCurrentMember(IHttpContextAccessor accessor) : ICurrentMember
{
    public Guid? MemberId => ReadGuid(Claims.Subject);

    public Guid? FamilyId => ReadGuid(ChoresClaims.FamilyId);

    public Role? Role => ChoresClaims.ParseRole(accessor.HttpContext?.User.FindFirst(Claims.Role)?.Value);

    private Guid? ReadGuid(string claim) =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirst(claim)?.Value, out var value) ? value : null;
}
