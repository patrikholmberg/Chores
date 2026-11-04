using Chores.Application.Common.Exceptions;
using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Domain.ChoreLibrary;
using Mediator;

namespace Chores.Application.Features.ChoreLibrary;

/// <summary>A Parent adds a Chore to the library. Without a Point Multiplier it is worth 1.</summary>
public sealed record CreateChoreCommand(
    string Title,
    string? Description,
    int? PointMultiplier,
    IReadOnlyList<StepInput> Steps) : ICommand<ChoreDto>, IChoreInput, IParentOnly;

public sealed class CreateChoreValidator : ChoreInputValidator<CreateChoreCommand>;

public sealed class CreateChoreHandler(IChoreRepository chores, ITenantContext tenant, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateChoreCommand, ChoreDto>
{
    public async ValueTask<ChoreDto> Handle(CreateChoreCommand command, CancellationToken cancellationToken)
    {
        var familyId = tenant.FamilyId ?? throw new ForbiddenException("No Family is selected.");
        var chore = Chore.Create(
            Guid.CreateVersion7(),
            familyId,
            command.Title,
            command.Description,
            command.PointMultiplier ?? Chore.DefaultPointMultiplier,
            command.Steps.Select(s => s.ToDefinition()).ToList());

        chores.Add(chore);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ChoreDto.From(chore);
    }
}
