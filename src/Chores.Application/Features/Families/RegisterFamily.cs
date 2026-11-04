using Chores.Application.Common.Identity;
using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Application.Common.Validation;
using Chores.Domain.Families;
using FluentValidation;
using Mediator;

namespace Chores.Application.Features.Families;

/// <summary>Open signup: creates a Family and its first Parent, who can then sign in.</summary>
public sealed record RegisterFamilyCommand(
    string FamilyName,
    string TimeZone,
    string Username,
    string? DisplayName,
    string Password,
    string? Email,
    string Language,
    string? Captcha) : ICommand<RegisterFamilyResult>;

public sealed record RegisterFamilyResult(Guid FamilyId, Guid MemberId);

public sealed class RegisterFamilyValidator : AbstractValidator<RegisterFamilyCommand>
{
    public RegisterFamilyValidator()
    {
        RuleFor(c => c.FamilyName).NotEmpty().WithErrorCode(ValidationCodes.Required)
            .MaximumLength(Family.NameMaxLength).WithErrorCode(ValidationCodes.TooLong);
        RuleFor(c => c.TimeZone).ValidTimeZone();
        RuleFor(c => c.Username).ValidUsername();
        RuleFor(c => c.DisplayName).DisplayName();
        RuleFor(c => c.Password).PasswordFor(_ => Role.Parent);
        RuleFor(c => c.Email).ValidEmail();
        RuleFor(c => c.Language).ValidLanguage();
        RuleFor(c => c.Captcha).NotEmpty().WithErrorCode(ValidationCodes.CaptchaInvalid);
    }
}

public sealed class RegisterFamilyHandler(
    ICaptchaVerifier captcha,
    IMemberCredentials credentials,
    IFamilyRepository families,
    IUnitOfWork unitOfWork,
    TenantContext tenant) : ICommandHandler<RegisterFamilyCommand, RegisterFamilyResult>
{
    public async ValueTask<RegisterFamilyResult> Handle(RegisterFamilyCommand command, CancellationToken cancellationToken)
    {
        // Verified first: the replay store commits on its own, before anything for the Family is staged.
        if (!await captcha.VerifyAsync(command.Captcha, cancellationToken))
        {
            throw ValidationFailures.For(nameof(command.Captcha), ValidationCodes.CaptchaInvalid, "The CAPTCHA was not solved.");
        }

        var username = Username.Create(command.Username);
        if (await credentials.IsUsernameTakenAsync(username, null, cancellationToken))
        {
            throw ValidationFailures.UsernameTaken();
        }

        var familyId = Guid.CreateVersion7();
        var parentId = Guid.CreateVersion7();
        using var scope = tenant.BeginScope(familyId);

        families.Add(Family.Register(
            familyId,
            command.FamilyName,
            FamilyTimeZone.Create(command.TimeZone),
            parentId,
            username,
            command.DisplayName,
            command.Email!,
            PreferredLanguage.Create(command.Language)));
        await credentials.CreateAsync(parentId, username, command.Email, command.Password, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterFamilyResult(familyId, parentId);
    }
}
