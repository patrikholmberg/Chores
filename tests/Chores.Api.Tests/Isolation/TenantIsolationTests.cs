using System.Net;
using System.Net.Http.Json;
using Chores.Api.Tests.Infrastructure;
using Chores.Application.Common.Identity;
using Chores.Infrastructure.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Chores.Api.Tests.Isolation;

/// <summary>A Parent of Family A must never read or change Family B, whatever ids they send.</summary>
public class TenantIsolationTests(PostgresApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<(SeededFamily A, SeededFamily B, HttpClient ParentOfA)> TwoFamiliesAsync()
    {
        var a = await factory.Services.SeedFamilyAsync();
        var b = await factory.Services.SeedFamilyAsync();
        return (a, b, await factory.AsAsync(a.Parent));
    }

    [Fact]
    public async Task Listing_members_returns_only_the_own_family()
    {
        var (a, b, parentOfA) = await TwoFamiliesAsync();

        var json = await parentOfA.GetStringAsync("/api/members", Ct);

        Assert.Contains(a.Child.Username, json);
        Assert.DoesNotContain(b.Child.Username, json);
        Assert.DoesNotContain(b.Parent.Username, json);
    }

    [Fact]
    public async Task Another_familys_member_is_not_found_by_id()
    {
        var (_, b, parentOfA) = await TwoFamiliesAsync();

        var response = await parentOfA.GetAsync($"/api/members/{b.Child.Id}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Another_familys_member_cannot_be_edited()
    {
        var (_, b, parentOfA) = await TwoFamiliesAsync();

        var response = await parentOfA.PutAsJsonAsync($"/api/members/{b.Child.Id}", new
        {
            username = b.Child.Username,
            displayName = "Hijacked",
            role = "child",
            language = "en",
        }, Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Child", (await FindAsync(b.Child.Id))!.DisplayName);
    }

    [Fact]
    public async Task Another_familys_member_cannot_be_removed()
    {
        var (_, b, parentOfA) = await TwoFamiliesAsync();

        var response = await parentOfA.DeleteAsync($"/api/members/{b.Child.Id}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(await FindAsync(b.Child.Id));
    }

    [Fact]
    public async Task Another_familys_member_password_cannot_be_set()
    {
        var (_, b, parentOfA) = await TwoFamiliesAsync();

        var response = await parentOfA.PutAsJsonAsync($"/api/members/{b.Child.Id}/password", new { password = "pwned" }, Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        Assert.True(await scope.ServiceProvider.GetRequiredService<IMemberCredentials>()
            .CheckPasswordAsync(b.Child.Id, b.Child.Password, Ct));
    }

    [Fact]
    public async Task A_member_added_with_a_foreign_family_id_lands_in_the_own_family()
    {
        var (a, b, parentOfA) = await TwoFamiliesAsync();

        var response = await parentOfA.PostAsJsonAsync("/api/members", new
        {
            familyId = b.Id,
            username = TestData.UniqueUsername(),
            role = "child",
            language = "sv",
            password = "abcd",
        }, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(Ct);
        Assert.Equal(a.Id, (await FindAsync(Guid.Parse(created!["id"].ToString()!)))!.FamilyId);
    }

    [Fact]
    public async Task The_profile_shows_the_own_family()
    {
        var (a, _, parentOfA) = await TwoFamiliesAsync();

        var me = await parentOfA.GetFromJsonAsync<Dictionary<string, object>>("/api/me", Ct);

        Assert.Equal(a.Id.ToString(), me!["familyId"].ToString());
    }

    private async Task<Domain.Families.Member?> FindAsync(Guid memberId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<MemberDirectory>().FindAsync(memberId, Ct);
    }
}
