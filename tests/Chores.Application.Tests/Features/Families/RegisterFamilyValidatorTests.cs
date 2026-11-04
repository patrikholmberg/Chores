using Chores.Application.Common.Validation;
using Chores.Application.Features.Families;

namespace Chores.Application.Tests.Features.Families;

public class RegisterFamilyValidatorTests
{
    private static readonly RegisterFamilyCommand Valid = new(
        "The Holmbergs", "Europe/Stockholm", "alex", "Alex", "long-enough-pw", "alex@example.com", "sv", "solution");

    private static IEnumerable<string> CodesFor(RegisterFamilyCommand command, string property) =>
        new RegisterFamilyValidator().Validate(command).Errors.Where(e => e.PropertyName == property).Select(e => e.ErrorCode);

    [Fact]
    public void A_complete_signup_is_valid() =>
        Assert.True(new RegisterFamilyValidator().Validate(Valid).IsValid);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Email_is_required(string? email) =>
        Assert.Contains(ValidationCodes.Required, CodesFor(Valid with { Email = email }, "Email"));

    [Fact]
    public void Email_must_look_like_one() =>
        Assert.Contains(ValidationCodes.EmailInvalid, CodesFor(Valid with { Email = "not-an-email" }, "Email"));

    [Theory]
    [InlineData("Mars/Olympus")]
    [InlineData("W. Europe Standard Time")]
    public void Time_zone_must_be_iana(string timeZone) =>
        Assert.Contains(ValidationCodes.TimeZoneInvalid, CodesFor(Valid with { TimeZone = timeZone }, "TimeZone"));

    [Fact]
    public void A_parent_password_needs_ten_characters() =>
        Assert.Contains(ValidationCodes.PasswordTooShort, CodesFor(Valid with { Password = "123456789" }, "Password"));

    [Fact]
    public void Username_format_is_checked() =>
        Assert.Contains(ValidationCodes.UsernameInvalid, CodesFor(Valid with { Username = "big alex" }, "Username"));

    [Fact]
    public void Language_must_be_supported() =>
        Assert.Contains(ValidationCodes.LanguageInvalid, CodesFor(Valid with { Language = "de" }, "Language"));

    [Fact]
    public void Captcha_is_required() =>
        Assert.Contains(ValidationCodes.CaptchaInvalid, CodesFor(Valid with { Captcha = null }, "Captcha"));
}
