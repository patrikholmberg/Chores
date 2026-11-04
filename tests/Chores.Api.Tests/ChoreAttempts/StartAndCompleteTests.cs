using System.Net;
using System.Net.Http.Json;
using Chores.Api.Tests.ChoreLibrary;
using Chores.Api.Tests.Infrastructure;

namespace Chores.Api.Tests.ChoreAttempts;

public class StartAndCompleteTests(PostgresApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<(SeededFamily Family, HttpClient Parent, HttpClient Child, ChoreView Chore)> WashClothesAsync()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var parent = await factory.AsAsync(seeded.Parent);
        var child = await factory.AsAsync(seeded.Child);
        return (seeded, parent, child, await parent.CreateAsync(ChoreApi.WashClothes(), Ct));
    }

    [Fact]
    public async Task A_child_starts_and_completes_wash_clothes_in_order()
    {
        var (seeded, _, child, chore) = await WashClothesAsync();

        var response = await child.PostAsJsonAsync("/api/attempts", new { choreId = chore.Id }, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var started = (await response.Content.ReadFromJsonAsync<AttemptView>(Ct))!;
        Assert.Equal($"/api/attempts/{started.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal((seeded.Child.Id, "open", 0), (started.ChildId, started.State, started.CurrentStepPosition));
        Assert.Equal(["Wash clothes", "Hang clothes", "Collect clothes"], started.Steps.Select(s => s.Title));
        Assert.Equal((90, 30), (started.Steps[1].ReminderDelayMinutes!.Value, started.Steps[1].RepeatIntervalMinutes!.Value));

        var afterWash = (await (await child.CompleteStepAsync(started.Id, 0, Ct)).Content.ReadFromJsonAsync<AttemptView>(Ct))!;
        Assert.Equal(1, afterWash.CurrentStepPosition);
        Assert.NotNull(afterWash.Steps[0].CompletedAt);

        (await child.CompleteStepAsync(started.Id, 1, Ct)).EnsureSuccessStatusCode();
        var done = (await (await child.CompleteStepAsync(started.Id, 2, Ct)).Content.ReadFromJsonAsync<AttemptView>(Ct))!;
        Assert.Equal("complete", done.State);
        Assert.NotNull(done.CompletedAt);
        Assert.Null(done.CurrentStepPosition);
    }

    [Fact]
    public async Task The_same_chore_can_be_started_twice()
    {
        var (_, _, child, chore) = await WashClothesAsync();

        var first = await child.StartAsync(chore.Id, Ct);
        var second = await child.StartAsync(chore.Id, Ct);

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(2, (await child.AttemptsAsync(Ct)).Count(a => a.State == "open"));
    }

    [Fact]
    public async Task A_parent_cannot_start_or_complete_an_attempt()
    {
        var (_, parent, child, chore) = await WashClothesAsync();
        var attempt = await child.StartAsync(chore.Id, Ct);

        Assert.Equal(HttpStatusCode.Forbidden, (await parent.PostAsJsonAsync("/api/attempts", new { choreId = chore.Id }, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await parent.CompleteStepAsync(attempt.Id, 0, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await parent.PostAsync($"/api/attempts/{attempt.Id}/restart", null, Ct)).StatusCode);
        Assert.Equal(0, (await child.GetAttemptAsync(attempt.Id, Ct)).CurrentStepPosition);
    }

    [Fact]
    public async Task Completing_out_of_order_or_twice_is_a_conflict()
    {
        var (_, _, child, chore) = await WashClothesAsync();
        var attempt = await child.StartAsync(chore.Id, Ct);
        (await child.CompleteStepAsync(attempt.Id, 0, Ct)).EnsureSuccessStatusCode();

        var skip = await child.CompleteStepAsync(attempt.Id, 2, Ct);
        var repeat = await child.CompleteStepAsync(attempt.Id, 0, Ct);

        Assert.Equal(HttpStatusCode.Conflict, skip.StatusCode);
        Assert.Equal("attempt.not_current_step", await AttemptApi.CodeOf(skip, Ct));
        Assert.Equal(HttpStatusCode.Conflict, repeat.StatusCode);
        Assert.Equal(1, (await child.GetAttemptAsync(attempt.Id, Ct)).CurrentStepPosition);
    }

    [Fact]
    public async Task An_archived_chore_cannot_be_started()
    {
        var (_, parent, child, chore) = await WashClothesAsync();
        (await parent.DeleteAsync($"/api/chores/{chore.Id}", Ct)).EnsureSuccessStatusCode();

        var response = await child.PostAsJsonAsync("/api/attempts", new { choreId = chore.Id }, Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(await child.AttemptsAsync(Ct));
    }

    [Fact]
    public async Task Editing_or_archiving_the_chore_after_the_start_leaves_the_attempt_unchanged()
    {
        var (_, parent, child, chore) = await WashClothesAsync();
        var attempt = await child.StartAsync(chore.Id, Ct);

        (await parent.PutAsJsonAsync($"/api/chores/{chore.Id}", new
        {
            revision = chore.Revision,
            title = "Laundry",
            pointMultiplier = 3,
            steps = new[] { new { title = "Do the laundry" } },
        }, Ct)).EnsureSuccessStatusCode();
        (await parent.DeleteAsync($"/api/chores/{chore.Id}", Ct)).EnsureSuccessStatusCode();

        var reloaded = await child.GetAttemptAsync(attempt.Id, Ct);
        Assert.Equal(("Wash clothes", 1, 3), (reloaded.Title, reloaded.PointMultiplier, reloaded.Steps.Length));
        Assert.Equal(90, reloaded.Steps[1].ReminderDelayMinutes);
        Assert.Equal("complete", (await child.CompleteAllAsync(reloaded, Ct)).State);
    }
}
