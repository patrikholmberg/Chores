using Chores.Domain.Common;

namespace Chores.Domain.Families;

/// <summary>The tenant: a household and its Members. Guards the rule that a Family always keeps at least one Parent.</summary>
public sealed class Family
{
    public const int NameMaxLength = 100;
    public const int DefaultBasePoints = 10;
    public const int MinBasePoints = 1;
    public const int MaxBasePoints = 1000;
    public const int DefaultAttemptWindowDays = 7;
    public const int MinAttemptWindowDays = 1;
    public const int MaxAttemptWindowDays = 30;

    private readonly List<Member> _members = [];

    private Family(Guid id, string name, FamilyTimeZone timeZone)
    {
        Id = id;
        Name = name;
        TimeZone = timeZone;
        BasePoints = DefaultBasePoints;
        AttemptWindow = TimeSpan.FromDays(DefaultAttemptWindowDays);
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    /// <summary>Defines the Family Day for every day boundary, reminder and Settlement.</summary>
    public FamilyTimeZone TimeZone { get; private set; }

    /// <summary>The unit value of one standard chore. Every other points quantity is a multiple of it.</summary>
    public int BasePoints { get; private set; }

    /// <summary>How long after its start a Chore Attempt may be completed before it is Abandoned. Whole days.</summary>
    public TimeSpan AttemptWindow { get; private set; }

    /// <summary>Incremented by every change that can affect the last-Parent rule, so concurrent changes conflict.</summary>
    public int Revision { get; private set; }

    /// <summary>The Family's current (not removed) Members.</summary>
    public IReadOnlyCollection<Member> Members => _members.Where(m => !m.IsRemoved).ToList();

    public static Family Register(
        Guid id,
        string name,
        FamilyTimeZone timeZone,
        Guid parentId,
        Username parentUsername,
        string? parentDisplayName,
        string parentEmail,
        PreferredLanguage parentLanguage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var trimmed = name.Trim();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(trimmed.Length, NameMaxLength, nameof(name));

        var family = new Family(id, trimmed, timeZone);
        family.AddMember(parentId, parentUsername, parentDisplayName, Role.Parent, parentEmail, parentLanguage);
        return family;
    }

    public Member AddMember(
        Guid id,
        Username username,
        string? displayName,
        Role role,
        string? email,
        PreferredLanguage preferredLanguage)
    {
        var member = Member.Create(id, Id, username, displayName, role, email, preferredLanguage);
        _members.Add(member);
        return member;
    }

    public void UpdateMember(Guid memberId, Username username, string? displayName, PreferredLanguage preferredLanguage) =>
        Find(memberId).Update(username, displayName, preferredLanguage);

    /// <summary>Changes a Member's role and email. Demoting the only Parent is refused.</summary>
    public void ChangeRole(Guid memberId, Role role, string? email)
    {
        var member = Find(memberId);
        if (member.Role == Role.Parent && role != Role.Parent)
        {
            EnsureAnotherParent(member);
        }

        member.SetRole(role, email);
        Revision++;
    }

    /// <summary>Removes a Member. Removing the only Parent is refused.</summary>
    public void RemoveMember(Guid memberId, DateTimeOffset removedAt)
    {
        var member = Find(memberId);
        if (member.Role == Role.Parent)
        {
            EnsureAnotherParent(member);
        }

        member.Remove(removedAt);
        Revision++;
    }

    /// <summary>
    /// Changes the Family-wide settings. Settings are last-write-wins, so this does not increment <see cref="Revision"/>.
    /// </summary>
    public void ChangeSettings(int basePoints, int attemptWindowDays)
    {
        if (basePoints is < MinBasePoints or > MaxBasePoints)
        {
            throw new DomainRuleViolation(
                FamilyErrorCodes.BasePointsOutOfRange,
                $"Base Points must be a whole number from {MinBasePoints} to {MaxBasePoints}.");
        }

        if (attemptWindowDays is < MinAttemptWindowDays or > MaxAttemptWindowDays)
        {
            throw new DomainRuleViolation(
                FamilyErrorCodes.AttemptWindowOutOfRange,
                $"The attempt window must be {MinAttemptWindowDays} to {MaxAttemptWindowDays} days.");
        }

        BasePoints = basePoints;
        AttemptWindow = TimeSpan.FromDays(attemptWindowDays);
    }

    private void EnsureAnotherParent(Member member)
    {
        if (!Members.Any(m => m.Role == Role.Parent && m.Id != member.Id))
        {
            throw new DomainRuleViolation(FamilyErrorCodes.LastParent, "A Family must keep at least one Parent.");
        }
    }

    private Member Find(Guid memberId) =>
        Members.SingleOrDefault(m => m.Id == memberId)
        ?? throw new DomainRuleViolation(FamilyErrorCodes.MemberNotFound, $"Member {memberId} is not in this Family.");

#pragma warning disable CS8618 // Required by EF Core.
    private Family()
    {
    }
#pragma warning restore CS8618
}
