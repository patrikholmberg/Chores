using Chores.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chores.MigrationService;

/// <summary>Applies pending EF Core migrations, then stops the host. A failure sets a non-zero exit code.</summary>
public sealed class MigrationWorker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime lifetime,
    ILogger<MigrationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ChoresDbContext>();

            await db.Database.MigrateAsync(stoppingToken);
            logger.LogInformation("Database migrations applied");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database migration failed");
            Environment.ExitCode = 1;
        }

        lifetime.StopApplication();
    }
}
