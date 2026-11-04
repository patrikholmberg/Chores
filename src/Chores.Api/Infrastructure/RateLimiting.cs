using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace Chores.Api.Infrastructure;

/// <summary>Per-client-IP fixed-window limits for the anonymous endpoints that are worth abusing.</summary>
internal static class RateLimiting
{
    public const string Signup = "signup";
    public const string UsernameCheck = "username-check";
    public const string Login = "login";

    public static IServiceCollection AddChoresRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RateLimitSettings>(configuration.GetSection(RateLimitSettings.Section));

        // The Api is only reachable through the reverse proxy, which sets X-Forwarded-For to the real client.
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(Signup, context => Partition(context, s => s.Signup));
            options.AddPolicy(UsernameCheck, context => Partition(context, s => s.UsernameCheck));
            options.AddPolicy(Login, context => Partition(context, s => s.Login));
        });

        return services;
    }

    private static RateLimitPartition<string> Partition(HttpContext context, Func<RateLimitSettings, FixedWindow> select)
    {
        var window = select(context.RequestServices.GetRequiredService<IOptionsMonitor<RateLimitSettings>>().CurrentValue);
        return RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = window.PermitLimit,
                Window = TimeSpan.FromSeconds(window.WindowSeconds),
                QueueLimit = 0,
            });
    }
}

internal sealed class RateLimitSettings
{
    public const string Section = "RateLimiting";

    public FixedWindow Signup { get; set; } = new() { PermitLimit = 5, WindowSeconds = 3600 };

    public FixedWindow UsernameCheck { get; set; } = new() { PermitLimit = 30, WindowSeconds = 60 };

    public FixedWindow Login { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60 };
}

internal sealed class FixedWindow
{
    public int PermitLimit { get; set; }

    public int WindowSeconds { get; set; }
}
