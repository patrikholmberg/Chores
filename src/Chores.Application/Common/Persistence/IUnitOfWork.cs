namespace Chores.Application.Common.Persistence;

/// <summary>Commits every pending change of the request, including credentials, in one transaction.</summary>
public interface IUnitOfWork
{
    /// <exception cref="Exceptions.UsernameTakenException">Another Member already has the username.</exception>
    /// <exception cref="Exceptions.ConcurrencyConflictException">Someone else changed the same Family first.</exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
