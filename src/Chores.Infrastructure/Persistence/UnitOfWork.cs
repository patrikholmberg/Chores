using Chores.Application.Common.Exceptions;
using Chores.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Chores.Infrastructure.Persistence;

internal sealed class UnitOfWork(ChoresDbContext db) : IUnitOfWork
{
    /// <summary>Identity's unique index on the normalized username.</summary>
    private const string UsernameIndex = "UserNameIndex";

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
                                           {
                                               SqlState: PostgresErrorCodes.UniqueViolation,
                                               ConstraintName: UsernameIndex,
                                           })
        {
            throw new UsernameTakenException(ex);
        }
    }
}
