using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Mediator;

namespace Chores.Application.Features.ChoreLibrary;

/// <summary>Deleting a Chore archives it: it leaves the library but is kept, with its Steps, and can be restored.</summary>
public sealed record ArchiveChoreCommand(Guid ChoreId) : ICommand, IParentOnly;

public sealed class ArchiveChoreHandler(IChoreRepository chores, IUnitOfWork unitOfWork, TimeProvider time) : ICommandHandler<ArchiveChoreCommand>
{
    public async ValueTask<Unit> Handle(ArchiveChoreCommand command, CancellationToken cancellationToken)
    {
        var chore = await chores.GetRequiredAsync(command.ChoreId, cancellationToken);
        chore.Archive(time.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
