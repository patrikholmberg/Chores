using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Chores.Infrastructure.Persistence;

/// <summary>Lets <c>dotnet ef</c> build the model without starting a host. The connection is never opened.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ChoresDbContext>
{
    public ChoresDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<ChoresDbContext>()
            .UseNpgsql("Host=localhost;Database=chores")
            .Options);
}
