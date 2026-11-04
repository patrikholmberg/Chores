using System.Security.Cryptography;
using Chores.Application.Common.Identity;
using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Infrastructure.Captcha;
using Chores.Infrastructure.Identity;
using Ixnas.AltchaNet;
using Chores.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Chores.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Name of the Postgres database resource declared in the AppHost.</summary>
    public const string DatabaseName = "choresdb";

    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        // Not pooled: the context depends on the scoped tenant. The connection string is read lazily so test hosts can override it.
        services.AddDbContext<ChoresDbContext>((provider, options) => options
            .UseNpgsql(provider.GetRequiredService<IConfiguration>().GetConnectionString(DatabaseName)
                ?? throw new InvalidOperationException($"Connection string '{DatabaseName}' is missing."))
            .UseOpenIddict<Guid>());
        builder.EnrichNpgsqlDbContext<ChoresDbContext>();

        services.TryAddScoped<ICurrentMember, NoCurrentMember>();
        services.TryAddScoped<TenantContext>();
        services.TryAddScoped<ITenantContext>(provider => provider.GetRequiredService<TenantContext>());

        services.AddIdentityCore<MemberCredential>(options =>
            {
                // Role-dependent minimum lengths (Parent 10, Child 4) are enforced by the Application validators.
                options.Password.RequiredLength = 4;
                options.Password.RequiredUniqueChars = 1;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = false;
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyz0123456789._-";
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddUserStore<MemberCredentialStore>();

        services.AddScoped<MemberDirectory>();
        services.AddScoped<OidcClientSeeder>();
        services.AddScoped<OperatorPasswordReset>();
        services.AddScoped<IMemberCredentials, MemberCredentials>();
        services.AddScoped<IFamilyRepository, FamilyRepository>();
        services.AddScoped<IChoreRepository, ChoreRepository>();
        services.AddScoped<IChoreAttemptRepository, ChoreAttemptRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddDataProtection()
            .SetApplicationName("Chores")
            .PersistKeysToDbContext<ChoresDbContext>();

        services.AddOpenIddict()
            .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<ChoresDbContext>().ReplaceDefaultEntities<Guid>());

        AddCaptcha(builder);

        return builder;
    }

    private static void AddCaptcha(IHostApplicationBuilder builder)
    {
        var options = builder.Configuration.GetSection(CaptchaOptions.Section).Get<CaptchaOptions>() ?? new CaptchaOptions();
        var isDevelopment = builder.Environment.IsDevelopment();

        // Resolved on first use, so hosts that never issue challenges (Worker, MigrationService) need no key.
        var key = new Lazy<AltchaKey>(() => AltchaKey.FromBytes(options.HmacKey is { Length: > 0 } configured
            ? Convert.FromBase64String(configured)
            : isDevelopment
                ? RandomNumberGenerator.GetBytes(64)
                : throw new InvalidOperationException($"{CaptchaOptions.Section}:{nameof(CaptchaOptions.HmacKey)} must be set outside Development.")));

        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddScoped<UsedCaptchaChallengeStore>();
        builder.Services.AddScoped(provider => Altcha.CreateService(new AltchaSha256Configuration
        {
            Key = key.Value,
            StoreFactory = provider.GetRequiredService<UsedCaptchaChallengeStore>,
            Expiry = AltchaExpiry.FromSeconds(options.ExpirySeconds),
        }));
        builder.Services.AddScoped<ICaptchaVerifier, AltchaCaptchaVerifier>();
    }

    /// <summary>Used by hosts without signed-in Members (Worker, MigrationService).</summary>
    private sealed class NoCurrentMember : ICurrentMember
    {
        public Guid? MemberId => null;

        public Guid? FamilyId => null;

        public Domain.Families.Role? Role => null;
    }
}
