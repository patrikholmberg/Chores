using Chores.Domain.ChoreLibrary;

namespace Chores.Application.Common.Persistence;

/// <summary>Loads and stores Chore aggregates, always with their Steps. Only the current tenant's Chores are ever visible.</summary>
public interface IChoreRepository
{
    /// <summary>A Chore of the current Family, archived or not, or null.</summary>
    Task<Chore?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The current Family's archived Chores, or those in the library.</summary>
    Task<IReadOnlyList<Chore>> ListAsync(bool archived, CancellationToken cancellationToken);

    void Add(Chore chore);
}
