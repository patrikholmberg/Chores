using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Domain.Families;
using Microsoft.EntityFrameworkCore;

namespace Chores.Infrastructure.Persistence;

internal sealed class FamilyRepository(ChoresDbContext db, ITenantContext tenant) : IFamilyRepository
{
    public async Task<Family?> GetCurrentAsync(CancellationToken cancellationToken) =>
        tenant.FamilyId is null
            ? null
            : await db.Families.Include(f => f.Members).SingleOrDefaultAsync(cancellationToken);

    public void Add(Family family) => db.Families.Add(family);
}
