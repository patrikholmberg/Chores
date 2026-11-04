using Chores.Application.Common.Exceptions;
using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Application.Common.Validation;
using Chores.Application.Features.Members;
using Chores.Domain.ChoreAttempts;
using FluentValidation;
using Mediator;

namespace Chores.Application.Features.ChoreAttempts;

/// <summary>A Parent overturns a complete attempt, until its attempt window has passed. It then earns no points.</summary>
public sealed record RejectAttemptCommand(Guid AttemptId, string? Reason) : ICommand<AttemptDto>, IParentOnly;

public sealed class RejectAttemptValidator : AbstractValidator<RejectAttemptCommand>
{
    public RejectAttemptValidator() =>
        RuleFor(c => c.Reason)
            .Must(reason => reason is null || reason.Trim().Length <= ChoreAttempt.RejectionReasonMaxLength)
            .WithErrorCode(ValidationCodes.RejectionReasonTooLong)
            .WithMessage($"A rejection reason can be at most {ChoreAttempt.RejectionReasonMaxLength} characters.");
}

public sealed class RejectAttemptHandler(
    IChoreAttemptRepository attempts,
    IFamilyRepository families,
    ICurrentMember currentMember,
    IUnitOfWork unitOfWork,
    TimeProvider time) : ICommandHandler<RejectAttemptCommand, AttemptDto>
{
    public async ValueTask<AttemptDto> Handle(RejectAttemptCommand command, CancellationToken cancellationToken)
    {
        var parentId = currentMember.MemberId ?? throw new ForbiddenException("Only a signed-in Parent can reject.");
        var attempt = await attempts.LoadVisibleAsync(command.AttemptId, currentMember, cancellationToken);
        var now = time.GetUtcNow();

        attempt.Reject(parentId, command.Reason, now);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return AttemptDto.From(attempt, new MemberNames(await families.GetRequiredAsync(cancellationToken)), now);
    }
}
