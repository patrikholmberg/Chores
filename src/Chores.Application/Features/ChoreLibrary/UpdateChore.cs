using Chores.Application.Common.Exceptions;
using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Application.Common.Validation;
using FluentValidation;
using Mediator;

namespace Chores.Application.Features.ChoreLibrary;

/// <summary>
/// A Parent replaces a Chore's fields and Steps. <see cref="Revision"/> is the one the Parent loaded, so a save based on
/// an outdated copy conflicts instead of overwriting another Parent's change.
/// </summary>
public sealed record UpdateChoreCommand(
    Guid ChoreId,
    int Revision,
    string Title,
    string? Description,
    int? PointMultiplier,
    IReadOnlyList<StepInput> Steps) : ICommand<ChoreDto>, IChoreInput, IParentOnly;

public sealed class UpdateChoreValidator : ChoreInputValidator<UpdateChoreCommand>
{
    public UpdateChoreValidator() =>
        RuleFor(c => c.PointMultiplier).NotNull().WithErrorCode(ValidationCodes.Required);
}

public sealed class UpdateChoreHandler(IChoreRepository chores, IUnitOfWork unitOfWork) : ICommandHandler<UpdateChoreCommand, ChoreDto>
{
    public async ValueTask<ChoreDto> Handle(UpdateChoreCommand command, CancellationToken cancellationToken)
    {
        var chore = await chores.GetRequiredAsync(command.ChoreId, cancellationToken);
        if (chore.Revision != command.Revision)
        {
            throw new ConcurrencyConflictException();
        }

        chore.Revise(command.Title, command.Description, command.PointMultiplier!.Value, command.Steps.Select(s => s.ToDefinition()).ToList());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ChoreDto.From(chore);
    }
}
