using System.Net;
using System.Net.Http.Json;
using Chores.Api.Tests.ChoreLibrary;
using Chores.Api.Tests.Infrastructure;
using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Domain.ChoreLibrary;
using Microsoft.Extensions.DependencyInjection;

namespace Chores.Api.Tests.Isolation;

/// <summary>Members of Family A must never read or change Family B's Chores, whatever ids they send.</summary>
public class ChoreIsolationTests(PostgresApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<(SeededFamily A, SeededFamily B, Guid ChoreOfB, HttpClient ParentOfA)> TwoFamiliesAsync()
    {
        var a = await factory.Services.SeedFamilyAsync();
        var b = await factory.Services.SeedFamilyAsync();
        await factory.Services.SeedChoreAsync(a.Id, "Chore of A");
        var choreOfB = await factory.Services.SeedChoreAsync(b.Id, "Chore of B");
        return (a, b, choreOfB, await factory.AsAsync(a.Parent));
    }

    [Fact]
    public async Task The_library_lists_only_the_own_familys_chores()
    {
        var (a, _, _, parentOfA) = await TwoFamiliesAsync();
        var childOfA = await factory.AsAsync(a.Child);

        Assert.Equal(["Chore of A"], (await parentOfA.LibraryAsync(Ct)).Select(c => c.Title));
        Assert.Equal(["Chore of A"], (await childOfA.LibraryAsync(Ct)).Select(c => c.Title));
    }

    [Fact]
    public async Task Another_familys_chore_is_not_found_by_id()
    {
        var (a, _, choreOfB, parentOfA) = await TwoFamiliesAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await parentOfA.GetAsync($"/api/chores/{choreOfB}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await (await factory.AsAsync(a.Child)).GetAsync($"/api/chores/{choreOfB}", Ct)).StatusCode);
    }

    [Fact]
    public async Task Another_familys_chore_cannot_be_edited_archived_or_restored()
    {
        var (_, b, choreOfB, parentOfA) = await TwoFamiliesAsync();

        var edit = await parentOfA.PutAsJsonAsync($"/api/chores/{choreOfB}", new
        {
            revision = 0,
            title = "Hijacked",
            pointMultiplier = 1,
            steps = new[] { new { title = "Hijacked" } },
        }, Ct);
        var archive = await parentOfA.DeleteAsync($"/api/chores/{choreOfB}", Ct);
        var restore = await parentOfA.PostAsync($"/api/chores/{choreOfB}/restore", null, Ct);

        Assert.All([edit, archive, restore], r => Assert.Equal(HttpStatusCode.NotFound, r.StatusCode));
        var chore = await FindAsync(b.Id, choreOfB);
        Assert.Equal("Chore of B", chore.Title);
        Assert.False(chore.IsArchived);
        Assert.Equal(0, chore.Revision);
    }

    [Fact]
    public async Task Another_familys_archived_chore_is_neither_listed_nor_restorable()
    {
        var (_, b, choreOfB, parentOfA) = await TwoFamiliesAsync();
        await ChangeAsync(b.Id, choreOfB, c => c.Archive(DateTimeOffset.UtcNow));

        var archived = await parentOfA.GetFromJsonAsync<ChoreSummaryView[]>("/api/chores/archived", Ct);
        var restore = await parentOfA.PostAsync($"/api/chores/{choreOfB}/restore", null, Ct);

        Assert.Empty(archived!);
        Assert.Equal(HttpStatusCode.NotFound, restore.StatusCode);
        Assert.True((await FindAsync(b.Id, choreOfB)).IsArchived);
    }

    [Fact]
    public async Task A_chore_created_with_a_foreign_family_id_lands_in_the_own_family()
    {
        var a = await factory.Services.SeedFamilyAsync();
        var b = await factory.Services.SeedFamilyAsync();
        var parentOfA = await factory.AsAsync(a.Parent);

        var created = await parentOfA.CreateAsync(new { familyId = b.Id, title = "Mine", steps = new[] { new { title = "Do it" } } }, Ct);

        Assert.Equal(a.Id, (await FindAsync(a.Id, created.Id)).FamilyId);
    }

    /// <summary>Reads a Chore as <paramref name="familyId"/>, bypassing the Api.</summary>
    private async Task<Chore> FindAsync(Guid familyId, Guid choreId)
    {
        Chore? found = null;
        await ChangeAsync(familyId, choreId, c => found = c);
        return found!;
    }

    private async Task ChangeAsync(Guid familyId, Guid choreId, Action<Chore> change)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        using var _ = provider.GetRequiredService<TenantContext>().BeginScope(familyId);
        change((await provider.GetRequiredService<IChoreRepository>().GetAsync(choreId, Ct))!);
        await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(Ct);
    }
}
