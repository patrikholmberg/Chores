using Chores.Infrastructure.Persistence;
using Microsoft.Extensions.Hosting;

namespace Chores.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Name of the Postgres database resource declared in the AppHost.</summary>
    public const string DatabaseName = "choresdb";

    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<ChoresDbContext>(DatabaseName);
        return builder;
    }
}
