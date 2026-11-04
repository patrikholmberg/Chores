using System.Net;
using System.Net.Http.Json;
using Chores.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;

namespace Chores.Api.Tests.SignIn;

public class AuthorizationFlowTests(PostgresApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_child_signs_in_with_only_username_and_password_and_gets_family_and_role_claims()
    {
        var seeded = await factory.Services.SeedFamilyAsync();

        var tokens = await factory.SignInAsync(seeded.Child.Username.ToUpperInvariant(), seeded.Child.Password);
        var principal = await factory.ValidateAccessTokenAsync(tokens.AccessToken);

        Assert.Equal(seeded.Child.Id.ToString(), principal.FindFirst("sub")?.Value);
        Assert.Equal(seeded.Id.ToString(), principal.FindFirst("family_id")?.Value);
        Assert.Equal("child", principal.FindFirst("role")?.Value);
        Assert.Equal(seeded.Child.Username, principal.FindFirst("name")?.Value);
        Assert.Null(principal.FindFirst("security_stamp"));
        Assert.InRange(tokens.ExpiresIn, 14 * 60, 15 * 60);
        Assert.False(string.IsNullOrEmpty(tokens.RefreshToken));
    }

    [Fact]
    public async Task Authorize_without_the_sign_in_cookie_redirects_to_the_sign_in_screen()
    {
        var (_, challenge) = OidcFlow.CreatePkce();
        var url = OidcFlow.AuthorizeUrl(challenge);

        var response = await factory.CreateBrowser().GetAsync(url, Ct);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        var location = response.Headers.Location!;
        Assert.Equal("/login", location.AbsolutePath);
        Assert.Equal(url, QueryHelpers.ParseQuery(location.Query)["returnUrl"].Single());
    }

    [Fact]
    public async Task The_sign_in_cookie_is_cleared_once_the_code_is_issued()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var browser = factory.CreateBrowser();
        await browser.LoginAsync(seeded.Parent.Username, seeded.Parent.Password);
        var (_, challenge) = OidcFlow.CreatePkce();

        var first = await browser.GetAsync(OidcFlow.AuthorizeUrl(challenge), Ct);
        var second = await browser.GetAsync(OidcFlow.AuthorizeUrl(challenge), Ct);

        Assert.StartsWith(OidcFlow.RedirectUri, first.Headers.Location!.ToString());
        Assert.Equal("/login", second.Headers.Location!.AbsolutePath);
    }

    [Theory]
    [InlineData("wrong-password", false)]
    [InlineData(TestData.ChildPassword, true)]
    public async Task Wrong_password_and_unknown_username_fail_the_same_way(string password, bool unknownUser)
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var username = unknownUser ? TestData.UniqueUsername() : seeded.Child.Username;

        var response = await factory.CreateBrowser().LoginAsync(username, password);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(Ct);
        Assert.Equal("signin.failed", problem!["code"].ToString());
        Assert.Equal("The username or password is incorrect.", problem["title"].ToString());
    }

    [Fact]
    public async Task An_authorization_request_without_PKCE_is_rejected()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var browser = factory.CreateBrowser();
        await browser.LoginAsync(seeded.Parent.Username, seeded.Parent.Password);

        var response = await browser.GetAsync(OidcFlow.AuthorizeUrl("", method: null), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(response.Headers.Location);
        Assert.Contains("code_challenge", await response.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task A_plain_PKCE_challenge_is_rejected()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var browser = factory.CreateBrowser();
        await browser.LoginAsync(seeded.Parent.Username, seeded.Parent.Password);

        var response = await browser.GetAsync(OidcFlow.AuthorizeUrl("plain-challenge-that-is-long-enough-1234567890", method: "plain"), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("invalid_request", await response.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task The_password_grant_is_rejected()
    {
        var seeded = await factory.Services.SeedFamilyAsync();

        var response = await factory.CreateClient().PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = OidcFlow.ClientId,
            ["username"] = seeded.Parent.Username,
            ["password"] = seeded.Parent.Password,
        }), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("unsupported_grant_type", await response.Content.ReadAsStringAsync(Ct));
    }
}
