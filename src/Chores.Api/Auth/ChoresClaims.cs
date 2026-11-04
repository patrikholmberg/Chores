using Chores.Domain.Families;

namespace Chores.Api.Auth;

/// <summary>Claims Chores adds on top of the standard OpenID Connect ones (<c>sub</c>, <c>name</c>, <c>role</c>).</summary>
internal static class ChoresClaims
{
    public const string FamilyId = "family_id";

    /// <summary>Kept only in the refresh-token principal, to detect password changes at refresh time.</summary>
    public const string SecurityStamp = "security_stamp";

    public static string RoleValue(Role role) => role switch
    {
        Role.Parent => "parent",
        Role.Child => "child",
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };

    public static Role? ParseRole(string? value) => value switch
    {
        "parent" => Role.Parent,
        "child" => Role.Child,
        _ => null,
    };
}
