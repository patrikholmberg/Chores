using Chores.Domain.Common;

namespace Chores.Domain.Families;

/// <summary>A person in a Family with their own login, either a Parent or a Child. Changed only through <see cref="Family"/>.</summary>
public sealed class Member : IFamilyOwned
{
    public const int DisplayNameMaxLength = 50;

    private Member(Guid id, Guid familyId, Username username, string displayName, Role role, string? email, PreferredLanguage preferredLanguage)
    {
        Id = id;
        FamilyId = familyId;
        Username = username;
        DisplayName = displayName;
        Role = role;
        Email = email;
        PreferredLanguage = preferredLanguage;
    }

    public Guid Id { get; private set; }

    public Guid FamilyId { get; private set; }

    public Username Username { get; private set; }

    public string DisplayName { get; private set; }

    public Role Role { get; private set; }

    /// <summary>Required for a Parent (used for password reset), always null for a Child.</summary>
    public string? Email { get; private set; }

    public PreferredLanguage PreferredLanguage { get; private set; }

    /// <summary>When the Member was removed. Removed Members are kept for history but can no longer sign in.</summary>
    public DateTimeOffset? RemovedAt { get; private set; }

    public bool IsRemoved => RemovedAt is not null;

    internal static Member Create(
        Guid id,
        Guid familyId,
        Username username,
        string? displayName,
        Role role,
        string? email,
        PreferredLanguage preferredLanguage) =>
        new(id, familyId, username, NormalizeDisplayName(displayName, username), role, CheckEmail(role, email), preferredLanguage);

    internal void Update(Username username, string? displayName, PreferredLanguage preferredLanguage)
    {
        Username = username;
        DisplayName = NormalizeDisplayName(displayName, username);
        PreferredLanguage = preferredLanguage;
    }

    /// <summary>Sets role and email together: a Parent must keep an email, and a Child never has one.</summary>
    internal void SetRole(Role role, string? email)
    {
        Email = CheckEmail(role, email);
        Role = role;
    }

    internal void Remove(DateTimeOffset removedAt) => RemovedAt = removedAt;

    public void ChangePreferredLanguage(PreferredLanguage preferredLanguage) => PreferredLanguage = preferredLanguage;

    private static string NormalizeDisplayName(string? displayName, Username username)
    {
        var name = string.IsNullOrWhiteSpace(displayName) ? username.Value : displayName.Trim();
        return name.Length <= DisplayNameMaxLength
            ? name
            : throw new ArgumentException($"Display name can be at most {DisplayNameMaxLength} characters.", nameof(displayName));
    }

    private static string? CheckEmail(Role role, string? email)
    {
        var trimmed = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        return (role, trimmed) switch
        {
            (Role.Parent, null) => throw new DomainRuleViolation(FamilyErrorCodes.EmailRequired, "A Parent must have an email address."),
            (Role.Child, not null) => throw new DomainRuleViolation(FamilyErrorCodes.EmailNotAllowed, "A Child cannot have an email address."),
            _ => trimmed,
        };
    }

#pragma warning disable CS8618 // Required by EF Core.
    private Member()
    {
    }
#pragma warning restore CS8618
}
