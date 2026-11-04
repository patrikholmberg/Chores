using Chores.Application.Common.Persistence;
using Chores.Domain.ChoreLibrary;
using Microsoft.EntityFrameworkCore;

namespace Chores.Infrastructure.Persistence;

internal sealed class ChoreRepository(ChoresDbContext db) : IChoreRepository
{
    public Task<Chore?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.Chores.Include(c => c.Steps).SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Chore>> ListAsync(bool archived, CancellationToken cancellationToken) =>
        await db.Chores.Include(c => c.Steps)
            .Where(c => (c.ArchivedAt != null) == archived)
            .ToListAsync(cancellationToken);

    public void Add(Chore chore) => db.Chores.Add(chore);
}
