using System.Net;
using System.Net.Http.Json;
using Chores.Api.Tests.Infrastructure;
using Chores.Domain.Families;
using Chores.Infrastructure.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Chores.Api.Tests.Members;

public class UpdateAndRemoveMemberTests(PostgresApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static object Update(SeededMember member, string role, string? email = null, string? password = null, string language = "en") => new
    {
        username = member.Username,
        displayName = "Renamed",
        role,
        email,
        language,
        password,
    };

    private static async Task<string?> CodeOf(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(Ct))?.GetValueOrDefault("code")?.ToString();

    [Fact]
    public async Task A_parent_edits_a_childs_name_and_language()
    {
        var seeded = await factory.Services.SeedFamilyAsync();

        var response = await (await factory.AsAsync(seeded.Parent))
            .PutAsJsonAsync($"/api/members/{seeded.Child.Id}", Update(seeded.Child, "child"), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var member = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(Ct);
        Assert.Equal("Renamed", member!["displayName"].ToString());
        Assert.Equal("en", member["preferredLanguage"].ToString());
    }

    [Fact]
    public async Task The_only_parent_cannot_remove_themselves()
    {
        var seeded = await factory.Services.SeedFamilyAsync();

        var response = await (await factory.AsAsync(seeded.Parent)).DeleteAsync($"/api/members/{seeded.Parent.Id}", Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("member.last_parent", await CodeOf(response));
    }

    [Fact]
    public async Task The_only_parent_cannot_demote_themselves()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var parent = await factory.AsAsync(seeded.Parent);

        var response = await parent.PutAsJsonAsync($"/api/members/{seeded.Parent.Id}", Update(seeded.Parent, "child"), Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("member.last_parent", await CodeOf(response));
        var me = await parent.GetFromJsonAsync<Dictionary<string, object>>("/api/me", Ct);
        Assert.Equal("parent", me!["role"].ToString());
    }

    [Fact]
    public async Task One_of_two_parents_can_be_demoted()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var second = await factory.Services.AddParentAsync(seeded.Id);

        var response = await (await factory.AsAsync(seeded.Parent))
            .PutAsJsonAsync($"/api/members/{second.Id}", Update(second, "child"), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("child", (await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(Ct))!["role"].ToString());
    }

    [Fact]
    public async Task Promoting_a_child_needs_a_parent_strength_password()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var parent = await factory.AsAsync(seeded.Parent);

        var withoutPassword = await parent.PutAsJsonAsync(
            $"/api/members/{seeded.Child.Id}", Update(seeded.Child, "parent", "kid@example.com"), Ct);
        var shortPassword = await parent.PutAsJsonAsync(
            $"/api/members/{seeded.Child.Id}", Update(seeded.Child, "parent", "kid@example.com", "abcd"), Ct);
        var promoted = await parent.PutAsJsonAsync(
            $"/api/members/{seeded.Child.Id}", Update(seeded.Child, "parent", "kid@example.com", "long-enough-pw"), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, withoutPassword.StatusCode);
        Assert.Contains("password.too_short", await withoutPassword.Content.ReadAsStringAsync(Ct));
        Assert.Equal(HttpStatusCode.BadRequest, shortPassword.StatusCode);
        Assert.Equal(HttpStatusCode.OK, promoted.StatusCode);
        await factory.SignInAsync(seeded.Child.Username, "long-enough-pw");
    }

    [Fact]
    public async Task A_removed_child_cannot_sign_in_or_refresh_and_disappears_from_the_list()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var childTokens = await factory.SignInAsync(seeded.Child.Username, seeded.Child.Password);
        var parent = await factory.AsAsync(seeded.Parent);

        var response = await parent.DeleteAsync($"/api/members/{seeded.Child.Id}", Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await factory.RefreshAsync(childTokens.RefreshToken)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await factory.CreateBrowser().LoginAsync(seeded.Child.Username, seeded.Child.Password)).StatusCode);
        var list = await parent.GetStringAsync("/api/members", Ct);
        Assert.DoesNotContain(seeded.Child.Username, list);
    }

    [Fact]
    public async Task Two_parents_demoting_each_other_at_once_leave_one_parent()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var second = await factory.Services.AddParentAsync(seeded.Id);
        var first = await factory.AsAsync(seeded.Parent);
        var other = await factory.AsAsync(second);

        var responses = await Task.WhenAll(
            first.PutAsJsonAsync($"/api/members/{second.Id}", Update(second, "child"), Ct),
            other.PutAsJsonAsync($"/api/members/{seeded.Parent.Id}", Update(seeded.Parent, "child"), Ct));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);

        await using var scope = factory.Services.CreateAsyncScope();
        var parents = 0;
        foreach (var id in new[] { seeded.Parent.Id, second.Id })
        {
            var member = await scope.ServiceProvider.GetRequiredService<MemberDirectory>().FindAsync(id, Ct);
            parents += member!.Role == Role.Parent ? 1 : 0;
        }

        Assert.Equal(1, parents);
    }
}
