using Chores.Domain.Families;
using Chores.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chores.Infrastructure.Identity;

/// <summary>
/// Finds Members across all Families for sign-in, token refresh and operator tools, which run before a Family is
/// known. This is the only place allowed to bypass the tenant query filter; removed Members are still hidden.
/// </summary>
public sealed class MemberDirectory(ChoresDbContext db)
{
    public Task<Member?> FindAsync(Guid memberId, CancellationToken cancellationToken) =>
        db.Members.IgnoreQueryFilters([ChoresDbContext.TenantFilter])
            .AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == memberId, cancellationToken);

    public Task<Member?> FindByUsernameAsync(Username username, CancellationToken cancellationToken) =>
        db.Members.IgnoreQueryFilters([ChoresDbContext.TenantFilter])
            .AsNoTracking()
            .SingleOrDefaultAsync(m => m.Username == username, cancellationToken);
}
