using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Chores.Api.Tests;

/// <summary>Hosts the Api with a connection string that points at a closed port.</summary>
public sealed class UnreachableDatabaseFactory : WebApplicationFactory<Program>
{
    public const string UnreachableHost = "127.0.0.1";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(
            "ConnectionStrings:choresdb",
            $"Host={UnreachableHost};Port=1;Database=chores;Username=x;Password=secret-pw;Timeout=2");

        // Without this, the health check retries the dead connection with backoff for minutes.
        builder.UseSetting("Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:DisableRetry", "true");
    }
}
