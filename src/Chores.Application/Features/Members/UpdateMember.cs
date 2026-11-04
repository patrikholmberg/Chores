using Chores.Application.Common.Identity;
using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Application.Common.Validation;
using Chores.Domain.Families;
using FluentValidation;
using Mediator;

namespace Chores.Application.Features.Members;

/// <summary>
/// A Parent edits a Member of their Family. Promoting a Child to Parent needs a new password that meets the Parent
/// policy; a password given otherwise replaces the current one and ends the Member's sessions.
/// </summary>
public sealed record UpdateMemberCommand(
    Guid MemberId,
    string Username,
    string? DisplayName,
    Role Role,
    string? Email,
    string Language,
    string? Password) : ICommand<MemberDto>, IParentOnly;

public sealed class UpdateMemberValidator : AbstractValidator<UpdateMemberCommand>
{
    public UpdateMemberValidator()
    {
        RuleFor(c => c.Username).ValidUsername();
        RuleFor(c => c.DisplayName).DisplayName();
        RuleFor(c => c.Role).IsInEnum().WithErrorCode(ValidationCodes.RoleInvalid);
        RuleFor(c => c.Email).EmailForRole(c => c.Role);
        RuleFor(c => c.Language).ValidLanguage();
        RuleFor(c => c.Password!).PasswordFor(c => c.Role).When(c => !string.IsNullOrEmpty(c.Password));
    }
}

public sealed class UpdateMemberHandler(
    IFamilyRepository families,
    IMemberCredentials credentials,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateMemberCommand, MemberDto>
{
    public async ValueTask<MemberDto> Handle(UpdateMemberCommand command, CancellationToken cancellationToken)
    {
        var family = await families.GetRequiredAsync(cancellationToken);
        var member = family.GetMember(command.MemberId);

        var username = Username.Create(command.Username);
        if (await credentials.IsUsernameTakenAsync(username, member.Id, cancellationToken))
        {
            throw ValidationFailures.UsernameTaken();
        }

        if (member.Role == Role.Child && command.Role == Role.Parent && string.IsNullOrEmpty(command.Password))
        {
            throw ValidationFailures.For(
                nameof(command.Password),
                ValidationCodes.PasswordTooShort,
                $"A new Parent needs a password of at least {PasswordPolicy.MinimumLength(Role.Parent)} characters.");
        }

        family.UpdateMember(member.Id, username, command.DisplayName, PreferredLanguage.Create(command.Language));
        family.ChangeRole(member.Id, command.Role, command.Email);
        await credentials.UpdateAsync(member.Id, username, member.Email, cancellationToken);

        var passwordChanged = !string.IsNullOrEmpty(command.Password);
        if (passwordChanged)
        {
            await credentials.SetPasswordAsync(member.Id, command.Password!, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        if (passwordChanged)
        {
            await credentials.RevokeSessionsAsync(member.Id, cancellationToken);
        }

        return MemberDto.From(member);
    }
}
