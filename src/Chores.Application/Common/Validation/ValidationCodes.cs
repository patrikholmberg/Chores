namespace Chores.Application.Common.Validation;

/// <summary>Stable error codes returned with validation problems, so clients can show translated messages.</summary>
public static class ValidationCodes
{
    public const string Required = "required";
    public const string TooLong = "too_long";
    public const string UsernameInvalid = "username.invalid";
    public const string UsernameTaken = "username.taken";
    public const string PasswordTooShort = "password.too_short";
    public const string PasswordIncorrect = "password.incorrect";
    public const string EmailInvalid = "email.invalid";
    public const string EmailNotAllowed = "email.not_allowed";
    public const string TimeZoneInvalid = "time_zone.invalid";
    public const string LanguageInvalid = "language.invalid";
    public const string RoleInvalid = "role.invalid";
    public const string CaptchaInvalid = "captcha.invalid";
}
