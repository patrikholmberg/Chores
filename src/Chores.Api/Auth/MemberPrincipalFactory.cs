using System.Security.Claims;
using Chores.Domain.Families;
using Chores.Infrastructure.Identity;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Chores.Api.Auth;

/// <summary>Builds the principal OpenIddict turns into tokens, always from the Member's current state.</summary>
internal static class MemberPrincipalFactory
{
    public static ClaimsPrincipal Create(Member member, MemberCredential credential, IEnumerable<string> scopes)
    {
        var identity = new ClaimsIdentity(
            authenticationType: "OpenIddict",
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.SetClaim(Claims.Subject, member.Id.ToString())
            .SetClaim(Claims.Name, member.Username.Value)
            .SetClaim(Claims.Role, ChoresClaims.RoleValue(member.Role))
            .SetClaim(ChoresClaims.FamilyId, member.FamilyId.ToString())
            .SetClaim(ChoresClaims.SecurityStamp, credential.SecurityStamp);

        identity.SetScopes(scopes);
        identity.SetDestinations(claim => claim.Type switch
        {
            // No destination: kept in the authorization code and refresh token only, never sent to the client.
            ChoresClaims.SecurityStamp => [],
            _ => [Destinations.AccessToken, Destinations.IdentityToken],
        });

        var principal = new ClaimsPrincipal(identity);
        principal.SetRefreshTokenLifetime(member.Role == Role.Child ? AuthSetup.ChildSessionLifetime : AuthSetup.ParentSessionLifetime);
        return principal;
    }
}
