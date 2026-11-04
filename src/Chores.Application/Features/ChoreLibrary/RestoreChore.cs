using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Mediator;

namespace Chores.Application.Features.ChoreLibrary;

/// <summary>Puts an archived Chore back in the library as it was when it was archived.</summary>
public sealed record RestoreChoreCommand(Guid ChoreId) : ICommand, IParentOnly;

public sealed class RestoreChoreHandler(IChoreRepository chores, IUnitOfWork unitOfWork) : ICommandHandler<RestoreChoreCommand>
{
    public async ValueTask<Unit> Handle(RestoreChoreCommand command, CancellationToken cancellationToken)
    {
        var chore = await chores.GetRequiredAsync(command.ChoreId, cancellationToken);
        chore.Restore();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
