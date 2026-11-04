using Chores.Application.Common.Persistence;
using Chores.Domain.ChoreAttempts;
using Microsoft.EntityFrameworkCore;

namespace Chores.Infrastructure.Persistence;

internal sealed class ChoreAttemptRepository(ChoresDbContext db) : IChoreAttemptRepository
{
    public Task<ChoreAttempt?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.ChoreAttempts.Include(a => a.Steps).SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ChoreAttempt>> ListAsync(Guid? childId, DateOnly fromDay, CancellationToken cancellationToken) =>
        await db.ChoreAttempts.Include(a => a.Steps)
            .Where(a => a.StartDay >= fromDay && (childId == null || a.ChildId == childId))
            .OrderByDescending(a => a.StartedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ChoreAttempt>> ListStartedOnAsync(DateOnly day, CancellationToken cancellationToken) =>
        await db.ChoreAttempts.Where(a => a.StartDay == day).ToListAsync(cancellationToken);

    public void Add(ChoreAttempt attempt) => db.ChoreAttempts.Add(attempt);

    public void Remove(ChoreAttempt attempt) => db.ChoreAttempts.Remove(attempt);
}
