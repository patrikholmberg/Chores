using System.Security.Cryptography.X509Certificates;
using Chores.Application.Common.Tenancy;
using Chores.Domain.Families;
using Chores.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Chores.Api.Auth;

/// <summary>OpenIddict authorization server, token validation for <c>/api</c> and the short-lived sign-in cookie.</summary>
internal static class AuthSetup
{
    public const string MemberPolicy = "Member";
    public const string ParentPolicy = "Parent";
    public const string ChildPolicy = "Child";

    public static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan ParentSessionLifetime = TimeSpan.FromDays(30);
    public static readonly TimeSpan ChildSessionLifetime = TimeSpan.FromDays(90);

    public static WebApplicationBuilder AddChoresAuth(this WebApplicationBuilder builder)
    {
        var options = builder.Configuration.GetSection(AuthOptions.Section).Get<AuthOptions>() ?? new AuthOptions();
        var services = builder.Services;

        services.AddIdentityCore<MemberCredential>().AddSignInManager();
        services.AddHttpContextAccessor();
        services.Replace(ServiceDescriptor.Scoped<ICurrentMember, HttpCurrentMember>());

        services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
            .AddCookie(IdentityConstants.ApplicationScheme, cookie =>
            {
                // Only bridges the sign-in screen to the authorize endpoint; the refresh token is the real session.
                cookie.Cookie.Name = "chores.signin";
                cookie.Cookie.HttpOnly = true;
                cookie.Cookie.SameSite = SameSiteMode.Strict;
                cookie.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                cookie.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                cookie.SlidingExpiration = false;
                cookie.LoginPath = "/login";
                cookie.ReturnUrlParameter = "returnUrl";
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(MemberPolicy, policy => policy
                .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .RequireClaim(ChoresClaims.FamilyId))
            .AddPolicy(ParentPolicy, policy => policy
                .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .RequireClaim(ChoresClaims.FamilyId)
                .RequireClaim(Claims.Role, ChoresClaims.RoleValue(Role.Parent)))
            .AddPolicy(ChildPolicy, policy => policy
                .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .RequireClaim(ChoresClaims.FamilyId)
                .RequireClaim(Claims.Role, ChoresClaims.RoleValue(Role.Child)));

        services.AddOpenIddict()
            .AddServer(server =>
            {
                server.SetAuthorizationEndpointUris("connect/authorize")
                    .SetTokenEndpointUris("connect/token")
                    .SetEndSessionEndpointUris("connect/endsession")
                    .SetRevocationEndpointUris("connect/revoke");

                server.AllowAuthorizationCodeFlow()
                    .AllowRefreshTokenFlow()
                    .RequireProofKeyForCodeExchange();

                // Only S256: "plain" would let anyone who sees the authorize URL redeem the code.
                server.Configure(o => o.CodeChallengeMethods.Remove(CodeChallengeMethods.Plain));

                server.RegisterScopes(Scopes.OpenId, Scopes.Profile, Scopes.OfflineAccess);
                server.RegisterClaims(Claims.Subject, Claims.Name, Claims.Role, ChoresClaims.FamilyId);

                server.SetAccessTokenLifetime(AccessTokenLifetime)
                    .SetRefreshTokenLifetime(ParentSessionLifetime)
                    .SetRefreshTokenReuseLeeway(TimeSpan.Zero);

                if (!string.IsNullOrWhiteSpace(options.Issuer))
                {
                    server.SetIssuer(new Uri(options.Issuer));
                }

                AddCredentials(server, options, builder.Environment);

                var aspNetCore = server.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough();
                if (builder.Environment.IsDevelopment())
                {
                    // The Vite dev proxy and the test server speak plain HTTP.
                    aspNetCore.DisableTransportSecurityRequirement();
                }
            })
            .AddValidation(validation =>
            {
                validation.UseLocalServer();
                validation.UseAspNetCore();
            });

        return builder;
    }

    private static void AddCredentials(OpenIddictServerBuilder server, AuthOptions options, IHostEnvironment environment)
    {
        if (environment.IsDevelopment()
            && string.IsNullOrWhiteSpace(options.SigningCertificate)
            && string.IsNullOrWhiteSpace(options.EncryptionCertificate))
        {
            server.AddDevelopmentEncryptionCertificate().AddDevelopmentSigningCertificate();
            return;
        }

        server.AddSigningCertificate(LoadCertificate(options.SigningCertificate, options.SigningCertificatePassword, nameof(options.SigningCertificate)))
            .AddEncryptionCertificate(LoadCertificate(options.EncryptionCertificate, options.EncryptionCertificatePassword, nameof(options.EncryptionCertificate)));
    }

    private static X509Certificate2 LoadCertificate(string? base64, string? password, string name) =>
        string.IsNullOrWhiteSpace(base64)
            ? throw new InvalidOperationException($"{AuthOptions.Section}:{name} must be set outside Development.")
            : X509CertificateLoader.LoadPkcs12(Convert.FromBase64String(base64), password, X509KeyStorageFlags.EphemeralKeySet);
}
