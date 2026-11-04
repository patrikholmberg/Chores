using Chores.Domain.Common;

namespace Chores.Domain.ChoreLibrary;

/// <summary>Trimming and length rules for the titles and descriptions of Chores and Steps.</summary>
internal static class Text
{
    public static string Required(string? value, int maxLength, string code, string owner)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) || trimmed.Length > maxLength
            ? throw new DomainRuleViolation(code, $"{owner} needs a title of 1 to {maxLength} characters.")
            : trimmed;
    }

    /// <summary>A blank value becomes null.</summary>
    public static string? Optional(string? value, int maxLength, string code, string owner)
    {
        var trimmed = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        return trimmed is { Length: var length } && length > maxLength
            ? throw new DomainRuleViolation(code, $"{owner} description can be at most {maxLength} characters.")
            : trimmed;
    }
}
