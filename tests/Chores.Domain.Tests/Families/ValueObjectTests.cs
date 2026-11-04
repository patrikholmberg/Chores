using System.Globalization;
using Chores.Domain.Families;

namespace Chores.Domain.Tests.Families;

public class UsernameTests
{
    [Theory]
    [InlineData("nova", "nova")]
    [InlineData("Nova", "nova")]
    [InlineData("  alex.b_2-x ", "alex.b_2-x")]
    [InlineData("abc", "abc")]
    public void Accepts_and_lower_cases_valid_usernames(string input, string expected)
    {
        Assert.True(Username.TryCreate(input, out var username));
        Assert.Equal(expected, username.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("big alex")]
    [InlineData("åsa")]
    [InlineData("alex@home")]
    [InlineData("abcdefghijklmnopqrstuvwxyz1234567")]
    public void Rejects_invalid_usernames(string? input) =>
        Assert.False(Username.TryCreate(input, out _));
}

public class FamilyTimeZoneTests
{
    [Theory]
    [InlineData("Europe/Stockholm")]
    [InlineData("America/New_York")]
    public void Accepts_iana_zones(string id)
    {
        Assert.True(FamilyTimeZone.TryCreate(id, out var timeZone));
        Assert.Equal(id, timeZone.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Mars/Olympus")]
    [InlineData("W. Europe Standard Time")]
    public void Rejects_unknown_and_windows_zones(string? id) =>
        Assert.False(FamilyTimeZone.TryCreate(id, out _));

    [Theory]
    // Winter (CET, UTC+1).
    [InlineData("2026-01-15T22:59:00Z", "2026-01-15")]
    [InlineData("2026-01-15T23:00:00Z", "2026-01-16")]
    // Summer (CEST, UTC+2).
    [InlineData("2026-07-01T21:59:00Z", "2026-07-01")]
    [InlineData("2026-07-01T22:00:00Z", "2026-07-02")]
    // Spring forward on 29 March: the evening is already CEST.
    [InlineData("2026-03-29T00:59:00Z", "2026-03-29")]
    [InlineData("2026-03-29T21:59:00Z", "2026-03-29")]
    [InlineData("2026-03-29T22:00:00Z", "2026-03-30")]
    // Fall back on 25 October: the evening is already CET.
    [InlineData("2026-10-25T22:59:00Z", "2026-10-25")]
    [InlineData("2026-10-25T23:00:00Z", "2026-10-26")]
    public void DayOf_uses_the_family_time_zone(string instant, string expected) =>
        Assert.Equal(
            DateOnly.Parse(expected, CultureInfo.InvariantCulture),
            FamilyTimeZone.Create("Europe/Stockholm").DayOf(DateTimeOffset.Parse(instant, CultureInfo.InvariantCulture)));
}

public class PreferredLanguageTests
{
    [Theory]
    [InlineData("sv")]
    [InlineData("en")]
    public void Accepts_swedish_and_english(string code)
    {
        Assert.True(PreferredLanguage.TryCreate(code, out var language));
        Assert.Equal(code, language.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("de")]
    [InlineData("SV")]
    [InlineData("en-US")]
    public void Rejects_other_languages(string? code) =>
        Assert.False(PreferredLanguage.TryCreate(code, out _));
}

public class PasswordPolicyTests
{
    [Theory]
    [InlineData(Role.Parent, 10)]
    [InlineData(Role.Child, 4)]
    public void Minimum_length_depends_on_role(Role role, int expected) =>
        Assert.Equal(expected, PasswordPolicy.MinimumLength(role));

    [Theory]
    [InlineData(Role.Parent, "123456789", false)]
    [InlineData(Role.Parent, "1234567890", true)]
    [InlineData(Role.Child, "abc", false)]
    [InlineData(Role.Child, "abcd", true)]
    [InlineData(Role.Child, null, false)]
    public void Only_length_is_checked(Role role, string? password, bool expected) =>
        Assert.Equal(expected, PasswordPolicy.IsSatisfiedBy(role, password));
}
