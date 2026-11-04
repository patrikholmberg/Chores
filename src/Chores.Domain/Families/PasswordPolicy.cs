namespace Chores.Domain.Families;

/// <summary>Length-only password rules: Parents need 10 characters, Children 4.</summary>
public static class PasswordPolicy
{
    public static int MinimumLength(Role role) => role switch
    {
        Role.Parent => 10,
        Role.Child => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };

    public static bool IsSatisfiedBy(Role role, string? password) =>
        password is not null && password.Length >= MinimumLength(role);
}
