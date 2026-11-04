using System.Net;
using Chores.Api.Tests.ChoreLibrary;
using Chores.Api.Tests.Infrastructure;

namespace Chores.Api.Tests.ChoreAttempts;

/// <summary>
/// Restart, cancel, expiry and rejection. Attempts seeded with a start in the past stand in for moving the clock, which the
/// test factory shares with every other test.
/// </summary>
public class RestartCancelRejectTests(PostgresApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static readonly TimeSpan Week = TimeSpan.FromDays(7);

    private async Task<(SeededFamily Family, HttpClient Parent, HttpClient Child, Guid ChoreId)> FamilyWithChoreAsync()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var choreId = await factory.Services.SeedChoreAsync(seeded.Id);
        return (seeded, await factory.AsAsync(seeded.Parent), await factory.AsAsync(seeded.Child), choreId);
    }

    [Fact]
    public async Task Restart_returns_to_the_first_step_and_keeps_the_start_day()
    {
        var (seeded, _, child, choreId) = await FamilyWithChoreAsync();
        var attemptId = await factory.Services.SeedAttemptAsync(seeded.Id, seeded.Child.Id, choreId, DateTimeOffset.UtcNow.AddDays(-2));
        (await child.CompleteStepAsync(attemptId, 0, Ct)).EnsureSuccessStatusCode();
        (await child.CompleteStepAsync(attemptId, 1, Ct)).EnsureSuccessStatusCode();
        var before = await child.GetAttemptAsync(attemptId, Ct);

        var response = await child.PostAsync($"/api/attempts/{attemptId}/restart", null, Ct);

        response.EnsureSuccessStatusCode();
        var after = await child.GetAttemptAsync(attemptId, Ct);
        Assert.Equal(0, after.CurrentStepPosition);
        Assert.All(after.Steps, s => Assert.Null(s.CompletedAt));
        Assert.Equal((before.StartDay, before.ExpiresAt), (after.StartDay, after.ExpiresAt));
        Assert.Equal("open", after.State);
    }

    [Fact]
    public async Task The_owner_and_a_parent_cancel_open_attempts()
    {
        var (seeded, parent, child, choreId) = await FamilyWithChoreAsync();
        var mine = await child.StartAsync(choreId, Ct);
        var other = await child.StartAsync(choreId, Ct);

        Assert.Equal(HttpStatusCode.NoContent, (await child.DeleteAsync($"/api/attempts/{mine.Id}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await parent.DeleteAsync($"/api/attempts/{other.Id}", Ct)).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await child.GetAsync($"/api/attempts/{mine.Id}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await parent.GetAsync($"/api/attempts/{other.Id}", Ct)).StatusCode);
        Assert.Empty(await child.AttemptsAsync(Ct));
        Assert.Equal(0, Assert.Single((await parent.TodayAsync(Ct)).Children, c => c.ChildId == seeded.Child.Id).Points);
    }

    [Fact]
    public async Task A_complete_attempt_cannot_be_cancelled_or_restarted()
    {
        var (_, _, child, choreId) = await FamilyWithChoreAsync();
        var attempt = await child.CompleteAllAsync(await child.StartAsync(choreId, Ct), Ct);

        var cancel = await child.DeleteAsync($"/api/attempts/{attempt.Id}", Ct);
        var restart = await child.PostAsync($"/api/attempts/{attempt.Id}/restart", null, Ct);

        Assert.Equal(HttpStatusCode.Conflict, cancel.StatusCode);
        Assert.Equal("attempt.not_open", await AttemptApi.CodeOf(cancel, Ct));
        Assert.Equal(HttpStatusCode.Conflict, restart.StatusCode);
        Assert.Equal("complete", (await child.GetAttemptAsync(attempt.Id, Ct)).State);
    }

    [Fact]
    public async Task An_attempt_left_open_past_its_window_is_abandoned()
    {
        var (seeded, _, child, choreId) = await FamilyWithChoreAsync();
        var attemptId = await factory.Services.SeedAttemptAsync(
            seeded.Id, seeded.Child.Id, choreId, DateTimeOffset.UtcNow - Week - TimeSpan.FromMinutes(1));

        Assert.Equal("abandoned", (await child.GetAttemptAsync(attemptId, Ct)).State);

        var complete = await child.CompleteStepAsync(attemptId, 0, Ct);
        Assert.Equal(HttpStatusCode.Conflict, complete.StatusCode);
        Assert.Equal("attempt.not_open", await AttemptApi.CodeOf(complete, Ct));
        Assert.Equal(HttpStatusCode.Conflict, (await child.DeleteAsync($"/api/attempts/{attemptId}", Ct)).StatusCode);
        Assert.Equal("abandoned", (await child.GetAttemptAsync(attemptId, Ct)).State);
    }

    [Fact]
    public async Task A_rejection_takes_the_attempt_out_of_todays_points()
    {
        var (seeded, parent, child, choreId) = await FamilyWithChoreAsync();
        await child.CompleteAllAsync(await child.StartAsync(choreId, Ct), Ct);
        var dirty = await child.CompleteAllAsync(await child.StartAsync(choreId, Ct), Ct);
        Assert.Equal(20, (await child.TodayAsync(Ct)).Children.Single().Points);

        var response = await parent.RejectAsync(dirty.Id, "Not clean", Ct);

        response.EnsureSuccessStatusCode();
        var rejected = await child.GetAttemptAsync(dirty.Id, Ct);
        Assert.Equal("rejected", rejected.State);
        Assert.Equal(("Not clean", seeded.Parent.Id), (rejected.Rejection!.Reason, rejected.Rejection.ByMemberId));
        Assert.Equal(10, (await child.TodayAsync(Ct)).Children.Single().Points);
    }

    [Fact]
    public async Task Rejection_is_refused_for_open_rejected_and_expired_attempts()
    {
        var (seeded, parent, child, choreId) = await FamilyWithChoreAsync();
        var open = await child.StartAsync(choreId, Ct);
        var once = await child.CompleteAllAsync(await child.StartAsync(choreId, Ct), Ct);
        (await parent.RejectAsync(once.Id, "First", Ct)).EnsureSuccessStatusCode();
        var expired = await factory.Services.SeedAttemptAsync(
            seeded.Id, seeded.Child.Id, choreId, DateTimeOffset.UtcNow - Week - TimeSpan.FromMinutes(1), complete: true);

        var rejectOpen = await parent.RejectAsync(open.Id, null, Ct);
        var rejectAgain = await parent.RejectAsync(once.Id, "Second", Ct);
        var rejectExpired = await parent.RejectAsync(expired, null, Ct);

        Assert.Equal((HttpStatusCode.Conflict, "attempt.not_complete"), (rejectOpen.StatusCode, await AttemptApi.CodeOf(rejectOpen, Ct)));
        Assert.Equal(HttpStatusCode.Conflict, rejectAgain.StatusCode);
        Assert.Equal("First", (await parent.GetAttemptAsync(once.Id, Ct)).Rejection!.Reason);
        Assert.Equal(
            (HttpStatusCode.Conflict, "attempt.rejection_window_closed"),
            (rejectExpired.StatusCode, await AttemptApi.CodeOf(rejectExpired, Ct)));
        Assert.Equal("complete", (await parent.GetAttemptAsync(expired, Ct)).State);
    }

    [Fact]
    public async Task A_child_cannot_reject()
    {
        var (_, _, child, choreId) = await FamilyWithChoreAsync();
        var attempt = await child.CompleteAllAsync(await child.StartAsync(choreId, Ct), Ct);

        Assert.Equal(HttpStatusCode.Forbidden, (await child.RejectAsync(attempt.Id, null, Ct)).StatusCode);
        Assert.Equal("complete", (await child.GetAttemptAsync(attempt.Id, Ct)).State);
    }

    [Fact]
    public async Task A_sibling_neither_sees_nor_works_another_childs_attempt()
    {
        var (seeded, parent, child, choreId) = await FamilyWithChoreAsync();
        var sibling = await factory.AsAsync(await factory.Services.AddChildAsync(seeded.Id));
        var attempt = await child.StartAsync(choreId, Ct);

        HttpResponseMessage[] responses =
        [
            await sibling.GetAsync($"/api/attempts/{attempt.Id}", Ct),
            await sibling.CompleteStepAsync(attempt.Id, 0, Ct),
            await sibling.PostAsync($"/api/attempts/{attempt.Id}/restart", null, Ct),
            await sibling.DeleteAsync($"/api/attempts/{attempt.Id}", Ct),
        ];

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.NotFound, r.StatusCode));
        Assert.Empty(await sibling.AttemptsAsync(Ct));
        Assert.Equal(0, (await child.GetAttemptAsync(attempt.Id, Ct)).CurrentStepPosition);

        // The sibling sees only their own points; the Parent sees both Children.
        Assert.Equal("Sibling", Assert.Single((await sibling.TodayAsync(Ct)).Children).ChildName);
        Assert.Equal(2, (await parent.TodayAsync(Ct)).Children.Length);
        Assert.Single(await parent.AttemptsAsync(Ct));
    }
}
