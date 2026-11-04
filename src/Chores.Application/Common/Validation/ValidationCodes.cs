using Chores.Domain.ChoreLibrary;

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

    // The Chore codes are the Domain's own, so a client translates a rule the same way whichever layer reports it.
    public const string MultiplierOutOfRange = ChoreErrorCodes.MultiplierOutOfRange;
    public const string NoSteps = ChoreErrorCodes.NoSteps;
    public const string TooManySteps = ChoreErrorCodes.TooManySteps;
    public const string DelayOnFirstStep = ChoreErrorCodes.FirstStepHasDelay;
    public const string DelayOutOfRange = ChoreErrorCodes.DelayOutOfRange;
    public const string IntervalOutOfRange = ChoreErrorCodes.IntervalOutOfRange;
    public const string IntervalWithoutDelay = ChoreErrorCodes.IntervalWithoutDelay;
}
