using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Chores.Api.Tests.Infrastructure;

namespace Chores.Api.Tests.ChoreLibrary;

public class EditAndArchiveTests(PostgresApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<(SeededFamily Family, HttpClient Parent, ChoreView Chore)> WashClothesAsync()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var parent = await factory.AsAsync(seeded.Parent);
        return (seeded, parent, await parent.CreateAsync(ChoreApi.WashClothes(), Ct));
    }

    private static async Task<string?> CodeOf(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).TryGetProperty("code", out var code) ? code.GetString() : null;

    [Fact]
    public async Task Reordered_steps_are_saved()
    {
        var (_, parent, chore) = await WashClothesAsync();
        StepView[] reordered = [chore.Steps[0], chore.Steps[2], chore.Steps[1]];

        var response = await parent.PutAsJsonAsync($"/api/chores/{chore.Id}", ChoreApi.EditOf(chore, steps: reordered), Ct);

        response.EnsureSuccessStatusCode();
        var saved = await parent.GetChoreAsync(chore.Id, Ct);
        Assert.Equal(["Wash clothes", "Collect clothes", "Hang clothes"], saved.Steps.Select(s => s.Title));
        Assert.Equal(chore.Revision + 1, saved.Revision);
    }

    [Fact]
    public async Task Moving_a_delayed_step_first_is_rejected_and_changes_nothing()
    {
        var (_, parent, chore) = await WashClothesAsync();
        StepView[] reordered = [chore.Steps[1], chore.Steps[0], chore.Steps[2]];

        var response = await parent.PutAsJsonAsync($"/api/chores/{chore.Id}", ChoreApi.EditOf(chore, steps: reordered), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Steps[0].ReminderDelayMinutes", await response.Content.ReadAsStringAsync(Ct));
        Assert.Equal(chore.Steps, (await parent.GetChoreAsync(chore.Id, Ct)).Steps);
    }

    [Fact]
    public async Task The_multiplier_can_be_changed()
    {
        var (_, parent, chore) = await WashClothesAsync();

        var response = await parent.PutAsJsonAsync($"/api/chores/{chore.Id}", new
        {
            revision = chore.Revision, title = chore.Title, pointMultiplier = 3, steps = chore.Steps,
        }, Ct);

        response.EnsureSuccessStatusCode();
        Assert.Equal(3, (await parent.GetChoreAsync(chore.Id, Ct)).PointMultiplier);
    }

    [Fact]
    public async Task A_save_based_on_an_outdated_copy_conflicts()
    {
        var (family, first, chore) = await WashClothesAsync();
        var second = await factory.AsAsync(await factory.Services.AddParentAsync(family.Id));

        (await first.PutAsJsonAsync($"/api/chores/{chore.Id}", ChoreApi.EditOf(chore, title: "First"), Ct)).EnsureSuccessStatusCode();
        var response = await second.PutAsJsonAsync($"/api/chores/{chore.Id}", ChoreApi.EditOf(chore, title: "Second"), Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("concurrency_conflict", await CodeOf(response));
        Assert.Equal("First", (await first.GetChoreAsync(chore.Id, Ct)).Title);
    }

    [Fact]
    public async Task Archiving_hides_the_chore_from_everyone_and_restoring_brings_it_back()
    {
        var (family, parent, chore) = await WashClothesAsync();
        var child = await factory.AsAsync(family.Child);

        Assert.Equal(HttpStatusCode.NoContent, (await parent.DeleteAsync($"/api/chores/{chore.Id}", Ct)).StatusCode);

        Assert.Empty(await parent.LibraryAsync(Ct));
        Assert.Empty(await child.LibraryAsync(Ct));
        Assert.Equal(HttpStatusCode.NotFound, (await child.GetAsync($"/api/chores/{chore.Id}", Ct)).StatusCode);
        Assert.True((await parent.GetChoreAsync(chore.Id, Ct)).Archived);
        var archived = await parent.GetFromJsonAsync<ChoreSummaryView[]>("/api/chores/archived", Ct);
        Assert.Equal([chore.Id], archived!.Select(c => c.Id));

        Assert.Equal(HttpStatusCode.NoContent, (await parent.PostAsync($"/api/chores/{chore.Id}/restore", null, Ct)).StatusCode);

        var restored = await child.GetChoreAsync(chore.Id, Ct);
        Assert.False(restored.Archived);
        Assert.Equal(chore.Steps, restored.Steps);
        Assert.Empty((await parent.GetFromJsonAsync<ChoreSummaryView[]>("/api/chores/archived", Ct))!);
    }

    [Fact]
    public async Task An_archived_chore_cannot_be_edited()
    {
        var (_, parent, chore) = await WashClothesAsync();
        (await parent.DeleteAsync($"/api/chores/{chore.Id}", Ct)).EnsureSuccessStatusCode();
        var archived = await parent.GetChoreAsync(chore.Id, Ct);

        var response = await parent.PutAsJsonAsync($"/api/chores/{chore.Id}", ChoreApi.EditOf(archived, title: "Laundry"), Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("chore.archived", await CodeOf(response));
        Assert.Equal("Wash clothes", (await parent.GetChoreAsync(chore.Id, Ct)).Title);
    }
}
