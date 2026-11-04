using System.Net;
using System.Net.Http.Json;
using Chores.Api.Tests.Infrastructure;

namespace Chores.Api.Tests.Members;

public class PasswordAndLanguageTests(PostgresApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_parent_resets_a_childs_password_and_the_old_one_stops_working()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var childSession = await factory.SignInAsync(seeded.Child.Username, seeded.Child.Password);

        var response = await (await factory.AsAsync(seeded.Parent))
            .PutAsJsonAsync($"/api/members/{seeded.Child.Id}/password", new { password = "efgh" }, Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await factory.SignInAsync(seeded.Child.Username, "efgh");
        Assert.Equal(HttpStatusCode.BadRequest, (await factory.CreateBrowser().LoginAsync(seeded.Child.Username, seeded.Child.Password)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await factory.RefreshAsync(childSession.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task A_parent_can_reset_another_parents_password_with_the_parent_policy()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var second = await factory.Services.AddParentAsync(seeded.Id);
        var parent = await factory.AsAsync(seeded.Parent);

        var tooShort = await parent.PutAsJsonAsync($"/api/members/{second.Id}/password", new { password = "abcd" }, Ct);
        var ok = await parent.PutAsJsonAsync($"/api/members/{second.Id}/password", new { password = "another-long-pw" }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, tooShort.StatusCode);
        Assert.Contains("password.too_short", await tooShort.Content.ReadAsStringAsync(Ct));
        Assert.Equal(HttpStatusCode.NoContent, ok.StatusCode);
    }

    [Fact]
    public async Task A_parent_changes_their_own_password_with_the_current_one()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var parent = await factory.AsAsync(seeded.Parent);

        var wrong = await parent.PutAsJsonAsync("/api/me/password", new { currentPassword = "nope", newPassword = "brand-new-password" }, Ct);
        var ok = await parent.PutAsJsonAsync("/api/me/password", new { currentPassword = seeded.Parent.Password, newPassword = "brand-new-password" }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        Assert.Contains("password.incorrect", await wrong.Content.ReadAsStringAsync(Ct));
        Assert.Equal(HttpStatusCode.NoContent, ok.StatusCode);
        await factory.SignInAsync(seeded.Parent.Username, "brand-new-password");
        Assert.Equal(HttpStatusCode.BadRequest, (await factory.CreateBrowser().LoginAsync(seeded.Parent.Username, seeded.Parent.Password)).StatusCode);
    }

    [Fact]
    public async Task A_child_cannot_change_their_own_password()
    {
        var seeded = await factory.Services.SeedFamilyAsync();

        var response = await (await factory.AsAsync(seeded.Child))
            .PutAsJsonAsync("/api/me/password", new { currentPassword = seeded.Child.Password, newPassword = "brand-new-password" }, Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task A_child_changes_their_own_language()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var child = await factory.AsAsync(seeded.Child);

        var response = await child.PutAsJsonAsync("/api/me/language", new { language = "en" }, Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var me = await child.GetFromJsonAsync<Dictionary<string, object>>("/api/me", Ct);
        Assert.Equal("en", me!["preferredLanguage"].ToString());
    }

    [Fact]
    public async Task An_unsupported_language_is_rejected()
    {
        var seeded = await factory.Services.SeedFamilyAsync();

        var response = await (await factory.AsAsync(seeded.Child)).PutAsJsonAsync("/api/me/language", new { language = "de" }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("language.invalid", await response.Content.ReadAsStringAsync(Ct));
    }
}
