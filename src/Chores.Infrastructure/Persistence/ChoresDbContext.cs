using Microsoft.EntityFrameworkCore;

namespace Chores.Infrastructure.Persistence;

public sealed class ChoresDbContext(DbContextOptions<ChoresDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChoresDbContext).Assembly);
    }
}
