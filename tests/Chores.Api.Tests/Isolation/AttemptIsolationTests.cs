using System.Net;
using System.Net.Http.Json;
using Chores.Api.Tests.ChoreAttempts;
using Chores.Api.Tests.Infrastructure;
using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Domain.ChoreAttempts;
using Microsoft.Extensions.DependencyInjection;

namespace Chores.Api.Tests.Isolation;

/// <summary>Members of Family A must never read or change Family B's Chore Attempts, whatever ids they send.</summary>
public class AttemptIsolationTests(PostgresApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<(SeededFamily A, SeededFamily B, Guid OpenOfB, Guid CompleteOfB, Guid ChoreOfB)> TwoFamiliesAsync()
    {
        var a = await factory.Services.SeedFamilyAsync();
        var b = await factory.Services.SeedFamilyAsync();
        var choreOfA = await factory.Services.SeedChoreAsync(a.Id, "Chore of A");
        var choreOfB = await factory.Services.SeedChoreAsync(b.Id, "Chore of B");
        await factory.Services.SeedAttemptAsync(a.Id, a.Child.Id, choreOfA);
        var openOfB = await factory.Services.SeedAttemptAsync(b.Id, b.Child.Id, choreOfB);
        var completeOfB = await factory.Services.SeedAttemptAsync(b.Id, b.Child.Id, choreOfB, complete: true);
        return (a, b, openOfB, completeOfB, choreOfB);
    }

    [Fact]
    public async Task Lists_and_todays_points_show_only_the_own_family()
    {
        var (a, _, _, _, _) = await TwoFamiliesAsync();
        var parentOfA = await factory.AsAsync(a.Parent);
        var childOfA = await factory.AsAsync(a.Child);

        Assert.Equal(["Chore of A"], (await parentOfA.AttemptsAsync(Ct)).Select(x => x.Title));
        Assert.Equal(["Chore of A"], (await childOfA.AttemptsAsync(Ct)).Select(x => x.Title));
        Assert.Equal([a.Child.Id], (await parentOfA.TodayAsync(Ct)).Children.Select(c => c.ChildId));
    }

    [Fact]
    public async Task Another_familys_attempt_is_not_found_and_unchanged()
    {
        var (a, b, openOfB, completeOfB, _) = await TwoFamiliesAsync();
        var parentOfA = await factory.AsAsync(a.Parent);
        var childOfA = await factory.AsAsync(a.Child);

        HttpResponseMessage[] responses =
        [
            await parentOfA.GetAsync($"/api/attempts/{openOfB}", Ct),
            await childOfA.GetAsync($"/api/attempts/{openOfB}", Ct),
            await childOfA.CompleteStepAsync(openOfB, 0, Ct),
            await childOfA.PostAsync($"/api/attempts/{openOfB}/restart", null, Ct),
            await childOfA.DeleteAsync($"/api/attempts/{openOfB}", Ct),
            await parentOfA.DeleteAsync($"/api/attempts/{openOfB}", Ct),
            await parentOfA.RejectAsync(completeOfB, "Not ours", Ct),
        ];

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.NotFound, r.StatusCode));
        var open = await FindAsync(b.Id, openOfB);
        Assert.NotNull(open);
        Assert.Equal(0, open.Revision);
        Assert.Null((await FindAsync(b.Id, completeOfB))!.RejectedAt);
    }

    [Fact]
    public async Task Another_familys_chore_cannot_be_started()
    {
        var (a, _, _, _, choreOfB) = await TwoFamiliesAsync();
        var childOfA = await factory.AsAsync(a.Child);

        var response = await childOfA.PostAsJsonAsync("/api/attempts", new { choreId = choreOfB }, Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Single(await childOfA.AttemptsAsync(Ct));
    }

    /// <summary>Reads an attempt as <paramref name="familyId"/>, bypassing the Api.</summary>
    private async Task<ChoreAttempt?> FindAsync(Guid familyId, Guid attemptId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        using var _ = scope.ServiceProvider.GetRequiredService<TenantContext>().BeginScope(familyId);
        return await scope.ServiceProvider.GetRequiredService<IChoreAttemptRepository>().GetAsync(attemptId, Ct);
    }
}
