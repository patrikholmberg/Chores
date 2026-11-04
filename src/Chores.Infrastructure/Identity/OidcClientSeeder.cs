using Microsoft.Extensions.Configuration;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Chores.Infrastructure.Identity;

/// <summary>Creates or updates the single OAuth client, the web app, with redirect URIs for its configured origins.</summary>
public sealed class OidcClientSeeder(IOpenIddictApplicationManager applications, IConfiguration configuration)
{
    public const string ClientId = "chores-web";
    public const string CallbackPath = "/auth/callback";

    /// <summary>Configuration key holding the public origins the web app is served from, e.g. <c>https://chores.example.com</c>.</summary>
    public const string WebOriginsKey = "Auth:WebOrigins";

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var origins = configuration.GetSection(WebOriginsKey).Get<string[]>() ?? [];
        if (origins.Length == 0)
        {
            throw new InvalidOperationException($"{WebOriginsKey} must list at least one origin.");
        }

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = ClientId,
            ClientType = ClientTypes.Public,
            ConsentType = ConsentTypes.Implicit,
            DisplayName = "Chores",
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.EndSession,
                Permissions.Endpoints.Revocation,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Profile,
                Permissions.Prefixes.Scope + Scopes.OfflineAccess,
            },
            Requirements = { Requirements.Features.ProofKeyForCodeExchange },
        };
        foreach (var origin in origins.Select(o => o.TrimEnd('/')))
        {
            descriptor.RedirectUris.Add(new Uri(origin + CallbackPath));
            descriptor.PostLogoutRedirectUris.Add(new Uri(origin + "/"));
        }

        var existing = await applications.FindByClientIdAsync(ClientId, cancellationToken);
        if (existing is null)
        {
            await applications.CreateAsync(descriptor, cancellationToken);
        }
        else
        {
            await applications.UpdateAsync(existing, descriptor, cancellationToken);
        }
    }
}
