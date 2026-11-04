using Chores.Application.Common.Identity;
using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Application.Common.Validation;
using Chores.Domain.Families;
using FluentValidation;
using Mediator;

namespace Chores.Application.Features.Members;

/// <summary>A Parent sets a new password for any Member of their Family, which ends that Member's sessions.</summary>
public sealed record SetMemberPasswordCommand(Guid MemberId, string Password) : ICommand, IParentOnly;

public sealed class SetMemberPasswordValidator : AbstractValidator<SetMemberPasswordCommand>
{
    public SetMemberPasswordValidator() =>
        RuleFor(c => c.Password).NotEmpty().WithErrorCode(ValidationCodes.Required);
}

public sealed class SetMemberPasswordHandler(
    IFamilyRepository families,
    IMemberCredentials credentials,
    IUnitOfWork unitOfWork) : ICommandHandler<SetMemberPasswordCommand>
{
    public async ValueTask<Unit> Handle(SetMemberPasswordCommand command, CancellationToken cancellationToken)
    {
        var member = (await families.GetRequiredAsync(cancellationToken)).GetMember(command.MemberId);
        await PasswordChange.ApplyAsync(member, command.Password, credentials, unitOfWork, cancellationToken);
        return Unit.Value;
    }
}

internal static class PasswordChange
{
    /// <summary>Checks the policy for the Member's role, sets the password, saves and ends every session of the Member.</summary>
    public static async Task ApplyAsync(
        Member member,
        string password,
        IMemberCredentials credentials,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        if (!PasswordPolicy.IsSatisfiedBy(member.Role, password))
        {
            throw ValidationFailures.For(
                "Password",
                ValidationCodes.PasswordTooShort,
                $"The password must be at least {PasswordPolicy.MinimumLength(member.Role)} characters.");
        }

        await credentials.SetPasswordAsync(member.Id, password, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await credentials.RevokeSessionsAsync(member.Id, cancellationToken);
    }
}
