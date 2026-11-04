using System.Net;
using System.Net.Http.Json;
using Chores.Api.Tests.Infrastructure;
using Chores.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Chores.Api.Tests.SignIn;

public class SessionTests(PostgresApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_protected_endpoint_without_a_token_returns_401()
    {
        var response = await factory.CreateClient().GetAsync("/api/me", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_garbage_token_returns_401()
    {
        var response = await factory.CreateClientWithToken("not-a-token").GetAsync("/api/me", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_revoked_refresh_token_no_longer_works()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var tokens = await factory.SignInAsync(seeded.Child.Username, seeded.Child.Password);

        var revoke = await factory.CreateClient().PostAsync("/connect/revoke", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = OidcFlow.ClientId,
            ["token"] = tokens.RefreshToken,
            ["token_type_hint"] = "refresh_token",
        }), Ct);
        var refresh = await factory.RefreshAsync(tokens.RefreshToken);

        Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, refresh.StatusCode);
    }

    [Fact]
    public async Task End_session_returns_to_the_registered_post_logout_uri()
    {
        var response = await factory.CreateBrowser().GetAsync(
            $"/connect/endsession?client_id={OidcFlow.ClientId}&post_logout_redirect_uri={Uri.EscapeDataString(PostgresApiFactory.Origin + "/")}",
            Ct);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal(PostgresApiFactory.Origin + "/", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Five_wrong_passwords_lock_sign_in_for_five_minutes_with_the_usual_error()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var browser = factory.CreateBrowser();
        for (var i = 0; i < 5; i++)
        {
            await browser.LoginAsync(seeded.Child.Username, "wrong");
        }

        var locked = await browser.LoginAsync(seeded.Child.Username, seeded.Child.Password);

        Assert.Equal(HttpStatusCode.BadRequest, locked.StatusCode);
        var problem = await locked.Content.ReadFromJsonAsync<Dictionary<string, object>>(Ct);
        Assert.Equal("signin.failed", problem!["code"].ToString());

        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<MemberCredential>>();
        var lockoutEnd = await users.GetLockoutEndDateAsync((await users.FindByIdAsync(seeded.Child.Id.ToString()))!);
        Assert.InRange(lockoutEnd!.Value - DateTimeOffset.UtcNow, TimeSpan.FromMinutes(4), TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task A_successful_sign_in_resets_the_failure_count()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var browser = factory.CreateBrowser();
        for (var i = 0; i < 4; i++)
        {
            await browser.LoginAsync(seeded.Child.Username, "wrong");
        }

        await browser.LoginAsync(seeded.Child.Username, seeded.Child.Password);
        await browser.LoginAsync(seeded.Child.Username, "wrong");
        var again = await browser.LoginAsync(seeded.Child.Username, seeded.Child.Password);

        Assert.Equal(HttpStatusCode.NoContent, again.StatusCode);
    }
}
