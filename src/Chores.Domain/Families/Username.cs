using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Chores.Domain.Families;

/// <summary>A Member's login name: 3–32 characters of a–z, digits, '.', '-' and '_', always lower case.</summary>
public sealed partial record Username
{
    public const int MinLength = 3;
    public const int MaxLength = 32;

    private Username(string value) => Value = value;

    public string Value { get; }

    public static bool TryCreate(string? input, [NotNullWhen(true)] out Username? username)
    {
        var normalized = input?.Trim().ToLowerInvariant();
        username = normalized is not null && Pattern().IsMatch(normalized) ? new Username(normalized) : null;
        return username is not null;
    }

    public static Username Create(string input) =>
        TryCreate(input, out var username)
            ? username
            : throw new ArgumentException($"'{input}' is not a valid username.", nameof(input));

    public override string ToString() => Value;

    [GeneratedRegex("^[a-z0-9._-]{3,32}$")]
    private static partial Regex Pattern();
}
