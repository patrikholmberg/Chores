using Chores.Application.Common.Exceptions;
using Chores.Application.Common.Identity;
using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Application.Common.Validation;
using Chores.Application.Features.Members;
using Chores.Domain.Families;
using FluentValidation;
using Mediator;

namespace Chores.Application.Features.Account;

/// <summary>
/// A Parent changes their own password by giving the current one. Children cannot: a Parent sets theirs.
/// All of the Parent's sessions end, including the one that made the change.
/// </summary>
public sealed record ChangeOwnPasswordCommand(string CurrentPassword, string NewPassword) : ICommand, IParentOnly;

public sealed class ChangeOwnPasswordValidator : AbstractValidator<ChangeOwnPasswordCommand>
{
    public ChangeOwnPasswordValidator()
    {
        RuleFor(c => c.CurrentPassword).NotEmpty().WithErrorCode(ValidationCodes.Required);
        RuleFor(c => c.NewPassword).PasswordFor(_ => Role.Parent);
    }
}

public sealed class ChangeOwnPasswordHandler(
    ICurrentMember currentMember,
    IFamilyRepository families,
    IMemberCredentials credentials,
    IUnitOfWork unitOfWork) : ICommandHandler<ChangeOwnPasswordCommand>
{
    public async ValueTask<Unit> Handle(ChangeOwnPasswordCommand command, CancellationToken cancellationToken)
    {
        var family = await families.GetCurrentAsync(cancellationToken);
        var member = family?.Members.SingleOrDefault(m => m.Id == currentMember.MemberId)
            ?? throw new NotFoundException("Member", currentMember.MemberId?.ToString() ?? "(none)");

        if (!await credentials.CheckPasswordAsync(member.Id, command.CurrentPassword, cancellationToken))
        {
            throw ValidationFailures.For(
                nameof(command.CurrentPassword), ValidationCodes.PasswordIncorrect, "The current password is incorrect.");
        }

        await credentials.SetPasswordAsync(member.Id, command.NewPassword, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await credentials.RevokeSessionsAsync(member.Id, cancellationToken);
        return Unit.Value;
    }
}
