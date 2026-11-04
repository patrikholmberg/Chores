using Chores.Domain.Families;
using FluentValidation;

namespace Chores.Application.Common.Validation;

/// <summary>Field rules shared by every command that accepts the same kind of input.</summary>
public static class RuleExtensions
{
    public static IRuleBuilderOptions<T, string> ValidUsername<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(value => Username.TryCreate(value, out _))
            .WithErrorCode(ValidationCodes.UsernameInvalid)
            .WithMessage($"Use {Username.MinLength}–{Username.MaxLength} characters: letters a–z, digits, '.', '-' or '_'.");

    public static IRuleBuilderOptions<T, string> ValidTimeZone<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(value => FamilyTimeZone.TryCreate(value, out _))
            .WithErrorCode(ValidationCodes.TimeZoneInvalid)
            .WithMessage("Choose a valid IANA time zone, such as Europe/Stockholm.");

    public static IRuleBuilderOptions<T, string> ValidLanguage<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(value => PreferredLanguage.TryCreate(value, out _))
            .WithErrorCode(ValidationCodes.LanguageInvalid)
            .WithMessage("The language must be 'sv' or 'en'.");

    public static IRuleBuilderOptions<T, string?> ValidEmail<T>(this IRuleBuilder<T, string?> rule) =>
        rule.NotEmpty().WithErrorCode(ValidationCodes.Required).WithMessage("An email address is required.")
            .MaximumLength(256).WithErrorCode(ValidationCodes.TooLong)
            .EmailAddress().WithErrorCode(ValidationCodes.EmailInvalid).WithMessage("Enter a valid email address.");

    /// <summary>A Parent needs a valid email; a Child must not have one.</summary>
    public static IRuleBuilderOptions<T, string?> EmailForRole<T>(this IRuleBuilder<T, string?> rule, Func<T, Role> role) =>
        rule.Must((command, email) => role(command) != Role.Child || string.IsNullOrWhiteSpace(email))
            .WithErrorCode(ValidationCodes.EmailNotAllowed).WithMessage("A Child cannot have an email address.")
            .Must((command, email) => role(command) != Role.Parent || !string.IsNullOrWhiteSpace(email))
            .WithErrorCode(ValidationCodes.Required).WithMessage("An email address is required for a Parent.")
            .MaximumLength(256).WithErrorCode(ValidationCodes.TooLong)
            .EmailAddress().WithErrorCode(ValidationCodes.EmailInvalid).WithMessage("Enter a valid email address.")
            .When(c => role(c) == Role.Parent, ApplyConditionTo.CurrentValidator);

    /// <summary>A required title of at most <paramref name="maxLength"/> characters.</summary>
    public static IRuleBuilderOptions<T, string> Title<T>(this IRuleBuilder<T, string> rule, int maxLength) =>
        rule.NotEmpty().WithErrorCode(ValidationCodes.Required).WithMessage("A title is required.")
            .MaximumLength(maxLength).WithErrorCode(ValidationCodes.TooLong);

    public static IRuleBuilderOptions<T, string?> DisplayName<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(Member.DisplayNameMaxLength).WithErrorCode(ValidationCodes.TooLong);

    /// <summary>The role-dependent minimum length from <see cref="PasswordPolicy"/>. An unknown role is reported by the role rule.</summary>
    public static IRuleBuilderOptions<T, string> PasswordFor<T>(this IRuleBuilder<T, string> rule, Func<T, Role> role) =>
        rule.Must((command, password) => !Enum.IsDefined(role(command)) || PasswordPolicy.IsSatisfiedBy(role(command), password))
            .WithErrorCode(ValidationCodes.PasswordTooShort)
            .WithMessage((command, _) => $"The password must be at least {PasswordPolicy.MinimumLength(role(command))} characters.");
}
