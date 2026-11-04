using Chores.Domain.ChoreAttempts;

namespace Chores.Application.Common.Persistence;

/// <summary>Loads and stores Chore Attempts. Only the current tenant's attempts are ever visible.</summary>
public interface IChoreAttemptRepository
{
    /// <summary>An attempt of the current Family with its Steps, or null.</summary>
    Task<ChoreAttempt?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Attempts started on <paramref name="fromDay"/> or later, of one Child or of all when <paramref name="childId"/> is
    /// null, newest first, with their Steps.
    /// </summary>
    Task<IReadOnlyList<ChoreAttempt>> ListAsync(Guid? childId, DateOnly fromDay, CancellationToken cancellationToken);

    /// <summary>Attempts started on the Family Day <paramref name="day"/>, without their Steps.</summary>
    Task<IReadOnlyList<ChoreAttempt>> ListStartedOnAsync(DateOnly day, CancellationToken cancellationToken);

    void Add(ChoreAttempt attempt);

    /// <summary>Deletes the attempt and its Steps.</summary>
    void Remove(ChoreAttempt attempt);
}
