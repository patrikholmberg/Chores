using System.Net;
using System.Net.Http.Json;
using Chores.Api.Tests.Infrastructure;

namespace Chores.Api.Tests.Members;

public class AddMemberTests(PostgresApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_parent_adds_a_child_who_can_then_sign_in()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var parent = await factory.AsAsync(seeded.Parent);
        var username = TestData.UniqueUsername();

        var response = await parent.PostAsJsonAsync("/api/members", new
        {
            username = username.ToUpperInvariant(),
            displayName = "Nova",
            role = "child",
            language = "sv",
            password = "abcd",
        }, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var member = await response.Content.ReadFromJsonAsync<Member>(Ct);
        Assert.Equal(username, member!.Username);
        Assert.Equal("child", member.Role);

        var tokens = await factory.SignInAsync(username, "abcd");
        var principal = await factory.ValidateAccessTokenAsync(tokens.AccessToken);
        Assert.Equal(seeded.Id.ToString(), principal.FindFirst("family_id")?.Value);
    }

    [Fact]
    public async Task The_member_list_shows_the_family_without_password_data()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        await factory.Services.SeedFamilyAsync();

        var json = await (await factory.AsAsync(seeded.Parent)).GetStringAsync("/api/members", Ct);
        var members = System.Text.Json.JsonSerializer.Deserialize<Member[]>(json, System.Text.Json.JsonSerializerOptions.Web);

        Assert.Equal([seeded.Parent.Username, seeded.Child.Username], members!.Select(m => m.Username));
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_username_used_in_another_family_is_rejected()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var other = await factory.Services.SeedFamilyAsync();

        var response = await (await factory.AsAsync(seeded.Parent)).PostAsJsonAsync("/api/members", new
        {
            username = other.Child.Username.ToUpperInvariant(),
            role = "child",
            language = "sv",
            password = "abcd",
        }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("username.taken", await response.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task A_parent_without_email_is_rejected()
    {
        var seeded = await factory.Services.SeedFamilyAsync();

        var response = await (await factory.AsAsync(seeded.Parent)).PostAsJsonAsync("/api/members", new
        {
            username = TestData.UniqueUsername(),
            role = "parent",
            language = "en",
            password = "long-enough-pw",
        }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("\"Email\"", await response.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task A_child_cannot_manage_members()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var child = await factory.AsAsync(seeded.Child);

        var add = await child.PostAsJsonAsync("/api/members", new
        {
            username = TestData.UniqueUsername(),
            role = "child",
            language = "sv",
            password = "abcd",
        }, Ct);
        var list = await child.GetAsync("/api/members", Ct);

        Assert.Equal(HttpStatusCode.Forbidden, add.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
    }

    private sealed record Member(Guid Id, string Username, string DisplayName, string Role, string PreferredLanguage, string? Email);
}
