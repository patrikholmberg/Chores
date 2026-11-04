using Chores.Application.Common.Exceptions;
using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Application.Common.Validation;
using Chores.Domain.Families;
using FluentValidation;
using Mediator;

namespace Chores.Application.Features.Account;

/// <summary>Any signed-in Member sets their own preferred language.</summary>
public sealed record SetOwnLanguageCommand(string Language) : ICommand;

public sealed class SetOwnLanguageValidator : AbstractValidator<SetOwnLanguageCommand>
{
    public SetOwnLanguageValidator() => RuleFor(c => c.Language).ValidLanguage();
}

public sealed class SetOwnLanguageHandler(
    ICurrentMember currentMember,
    IFamilyRepository families,
    IUnitOfWork unitOfWork) : ICommandHandler<SetOwnLanguageCommand>
{
    public async ValueTask<Unit> Handle(SetOwnLanguageCommand command, CancellationToken cancellationToken)
    {
        var family = await families.GetCurrentAsync(cancellationToken);
        var member = family?.Members.SingleOrDefault(m => m.Id == currentMember.MemberId)
            ?? throw new NotFoundException("Member", currentMember.MemberId?.ToString() ?? "(none)");

        member.ChangePreferredLanguage(PreferredLanguage.Create(command.Language));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
