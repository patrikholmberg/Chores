using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Validation;

namespace Chores.Api.Tests.Infrastructure;

public sealed record TokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("refresh_token")] string RefreshToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn);

/// <summary>Drives the same Authorization Code + PKCE flow as the web app, without a browser.</summary>
public static class OidcFlow
{
    public const string ClientId = "chores-web";
    public const string RedirectUri = PostgresApiFactory.Origin + "/auth/callback";
    public const string Scope = "openid profile offline_access";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>A client that keeps cookies but does not follow redirects, so each step of the flow is visible.</summary>
    public static HttpClient CreateBrowser(this PostgresApiFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    public static Task<HttpResponseMessage> LoginAsync(this HttpClient browser, string username, string password) =>
        browser.PostAsJsonAsync("/api/account/login", new { username, password }, Ct);

    public static string AuthorizeUrl(string codeChallenge, string? method = "S256")
    {
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = RedirectUri,
            ["scope"] = Scope,
            ["state"] = "state-1",
        };
        if (method is not null)
        {
            query["code_challenge"] = codeChallenge;
            query["code_challenge_method"] = method;
        }

        return QueryHelpers.AddQueryString("/connect/authorize", query);
    }

    public static (string Verifier, string Challenge) CreatePkce()
    {
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        return (verifier, Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))));
    }

    /// <summary>Signs in through the login endpoint and exchanges the authorization code for tokens.</summary>
    public static async Task<TokenResponse> SignInAsync(this PostgresApiFactory factory, string username, string password)
    {
        var browser = factory.CreateBrowser();
        var login = await browser.LoginAsync(username, password);
        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);

        var (verifier, challenge) = CreatePkce();
        var authorize = await browser.GetAsync(AuthorizeUrl(challenge), Ct);
        Assert.Equal(HttpStatusCode.Found, authorize.StatusCode);
        var callback = authorize.Headers.Location!;
        Assert.StartsWith(RedirectUri, callback.ToString());
        var code = QueryHelpers.ParseQuery(callback.Query)["code"].Single()!;

        var token = await browser.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = ClientId,
            ["code"] = code,
            ["redirect_uri"] = RedirectUri,
            ["code_verifier"] = verifier,
        }), Ct);
        Assert.True(token.IsSuccessStatusCode, await token.Content.ReadAsStringAsync(Ct));
        return (await token.Content.ReadFromJsonAsync<TokenResponse>(Ct))!;
    }

    public static Task<HttpResponseMessage> RefreshAsync(this PostgresApiFactory factory, string refreshToken) =>
        factory.CreateClient().PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = ClientId,
            ["refresh_token"] = refreshToken,
        }), Ct);

    /// <summary>Decrypts and validates an access token the way the Api does, to inspect its claims.</summary>
    public static async Task<ClaimsPrincipal> ValidateAccessTokenAsync(this PostgresApiFactory factory, string accessToken)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<OpenIddictValidationService>()
            .ValidateAccessTokenAsync(accessToken, Ct);
    }

    public static HttpClient CreateClientWithToken(this PostgresApiFactory factory, string accessToken)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
