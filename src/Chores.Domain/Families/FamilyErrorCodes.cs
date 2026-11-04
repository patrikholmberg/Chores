namespace Chores.Domain.Families;

/// <summary>Stable codes of <see cref="Common.DomainRuleViolation"/>s raised by the Family aggregate.</summary>
public static class FamilyErrorCodes
{
    public const string LastParent = "member.last_parent";
    public const string EmailRequired = "member.email_required";
    public const string EmailNotAllowed = "member.email_not_allowed";
    public const string MemberNotFound = "member.not_found";
    public const string BasePointsOutOfRange = "family.base_points_out_of_range";
    public const string AttemptWindowOutOfRange = "family.attempt_window_out_of_range";
}
