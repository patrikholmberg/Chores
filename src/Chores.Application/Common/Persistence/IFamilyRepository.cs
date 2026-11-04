using Chores.Domain.Families;

namespace Chores.Application.Common.Persistence;

/// <summary>Loads and stores Family aggregates. Only the current tenant's Family is ever visible.</summary>
public interface IFamilyRepository
{
    /// <summary>The current tenant's Family with its current Members, or null when there is no current tenant.</summary>
    Task<Family?> GetCurrentAsync(CancellationToken cancellationToken);

    void Add(Family family);
}
