using Chores.Infrastructure.Identity;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Chores.Api.Auth;

/// <summary>The passthrough handlers of the OpenIddict authorization and token endpoints.</summary>
internal static class ConnectEndpoints
{
    public static IEndpointRouteBuilder MapConnectEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapMethods("/connect/authorize", [HttpMethods.Get, HttpMethods.Post], AuthorizeAsync).AllowAnonymous();
        app.MapPost("/connect/token", TokenAsync).AllowAnonymous();
        app.MapMethods("/connect/endsession", [HttpMethods.Get, HttpMethods.Post], (Delegate)EndSessionAsync).AllowAnonymous();
        return app;
    }

    /// <summary>Clears any leftover sign-in cookie and returns to the client's post-logout URI.</summary>
    private static async Task<IResult> EndSessionAsync(HttpContext context)
    {
        await context.SignOutAsync(IdentityConstants.ApplicationScheme);
        return Results.SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
    }

    /// <summary>
    /// Issues an authorization code for the Member signed in by the short-lived cookie, or sends the browser to the
    /// sign-in screen, which returns here afterwards. The cookie is cleared once the code is issued.
    /// </summary>
    private static async Task<IResult> AuthorizeAsync(
        HttpContext context,
        UserManager<MemberCredential> users,
        MemberDirectory directory,
        CancellationToken cancellationToken)
    {
        var request = context.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        var cookie = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        var credential = cookie.Succeeded ? await users.GetUserAsync(cookie.Principal) : null;
        var member = credential is null ? null : await directory.FindAsync(credential.Id, cancellationToken);
        if (credential is null || member is null)
        {
            var parameters = context.Request.HasFormContentType ? [.. context.Request.Form] : context.Request.Query.ToList();
            return Results.Challenge(
                new AuthenticationProperties { RedirectUri = context.Request.PathBase + context.Request.Path + QueryString.Create(parameters) },
                [IdentityConstants.ApplicationScheme]);
        }

        await context.SignOutAsync(IdentityConstants.ApplicationScheme);
        return Results.SignIn(
            MemberPrincipalFactory.Create(member, credential, request.GetScopes()),
            authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Redeems codes and refresh tokens. The Member is reloaded every time, so removal, password changes and role
    /// changes take effect at the next refresh.
    /// </summary>
    private static async Task<IResult> TokenAsync(
        HttpContext context,
        UserManager<MemberCredential> users,
        MemberDirectory directory,
        CancellationToken cancellationToken)
    {
        var request = context.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");
        if (!request.IsAuthorizationCodeGrantType() && !request.IsRefreshTokenGrantType())
        {
            throw new InvalidOperationException("The grant type is not supported.");
        }

        var result = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var subject = result.Principal?.GetClaim(Claims.Subject);
        var credential = subject is null ? null : await users.FindByIdAsync(subject);
        var member = credential is null ? null : await directory.FindAsync(credential.Id, cancellationToken);

        if (credential is null || member is null
            || result.Principal!.GetClaim(ChoresClaims.SecurityStamp) != credential.SecurityStamp)
        {
            return Results.Forbid(
                new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The session is no longer valid.",
                }),
                [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
        }

        return Results.SignIn(
            MemberPrincipalFactory.Create(member, credential, result.Principal!.GetScopes()),
            authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
