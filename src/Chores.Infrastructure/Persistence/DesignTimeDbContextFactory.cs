using Chores.Application.Common.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Chores.Infrastructure.Persistence;

/// <summary>Lets <c>dotnet ef</c> build the model without starting a host. The connection is never opened.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ChoresDbContext>
{
    public ChoresDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ChoresDbContext>();
        options.UseNpgsql("Host=localhost;Database=chores").UseOpenIddict<Guid>();
        return new ChoresDbContext(options.Options, new NoTenant());
    }

    private sealed class NoTenant : ITenantContext
    {
        public Guid? FamilyId => null;
    }
}
