using Chores.Infrastructure.Identity;
using Chores.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(Chores.Api.Tests.Infrastructure.PostgresApiFactory))]

namespace Chores.Api.Tests.Infrastructure;

/// <summary>Hosts the Api against a real, migrated Postgres in a container, shared by every test in the assembly.</summary>
public sealed class PostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>The test server's origin, registered as the web app origin of the OAuth client.</summary>
    public const string Origin = "http://localhost";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ChoresDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<OidcClientSeeder>().SeedAsync(CancellationToken.None);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:choresdb", _postgres.GetConnectionString());
        builder.UseSetting($"{OidcClientSeeder.WebOriginsKey}:0", Origin);

        // Every test client shares one remote address, so the real limits would throttle the suite.
        foreach (var policy in new[] { "Signup", "UsernameCheck", "Login" })
        {
            builder.UseSetting($"RateLimiting:{policy}:PermitLimit", "100000");
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
