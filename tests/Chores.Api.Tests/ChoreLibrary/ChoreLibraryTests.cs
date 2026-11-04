using System.Net;
using System.Net.Http.Json;
using Chores.Api.Tests.Infrastructure;

namespace Chores.Api.Tests.ChoreLibrary;

public class ChoreLibraryTests(PostgresApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_parent_creates_a_chore_that_a_child_can_list_and_view()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var parent = await factory.AsAsync(seeded.Parent);
        var child = await factory.AsAsync(seeded.Child);

        var response = await parent.PostAsJsonAsync("/api/chores", ChoreApi.WashClothes(), Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<ChoreView>(Ct))!;
        Assert.Equal($"/api/chores/{created.Id}", response.Headers.Location?.OriginalString);

        var library = await child.LibraryAsync(Ct);
        var summary = Assert.Single(library);
        Assert.Equal(("Wash clothes", 1, 3), (summary.Title, summary.PointMultiplier, summary.StepCount));

        var chore = await child.GetChoreAsync(created.Id, Ct);
        Assert.Equal(["Wash clothes", "Hang clothes", "Collect clothes"], chore.Steps.Select(s => s.Title));
        Assert.Equal(new StepView("Hang clothes", null, 90, 30), chore.Steps[1]);
        Assert.Equal(new StepView("Collect clothes", null, 2880, null), chore.Steps[2]);
        Assert.Null(chore.Steps[0].ReminderDelayMinutes);
    }

    [Fact]
    public async Task The_library_is_sorted_by_title_and_starts_empty()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var parent = await factory.AsAsync(seeded.Parent);
        Assert.Empty(await parent.LibraryAsync(Ct));

        await factory.Services.SeedChoreAsync(seeded.Id, "Wash clothes");
        await factory.Services.SeedChoreAsync(seeded.Id, "Empty the dishwasher");

        Assert.Equal(["Empty the dishwasher", "Wash clothes"], (await parent.LibraryAsync(Ct)).Select(c => c.Title));
    }

    [Fact]
    public async Task A_chore_without_a_multiplier_is_worth_1()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var parent = await factory.AsAsync(seeded.Parent);

        var chore = await parent.CreateAsync(ChoreApi.WashClothes(pointMultiplier: null), Ct);

        Assert.Equal(1, chore.PointMultiplier);
    }

    [Fact]
    public async Task A_fractional_multiplier_is_rejected()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var parent = await factory.AsAsync(seeded.Parent);

        var response = await parent.PostAsync(
            "/api/chores",
            JsonContent.Create(new { title = "Wash clothes", pointMultiplier = 1.5, steps = new[] { new { title = "Wash" } } }),
            Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await parent.LibraryAsync(Ct));
    }

    [Fact]
    public async Task Field_errors_name_the_step_and_field()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var parent = await factory.AsAsync(seeded.Parent);

        var response = await parent.PostAsJsonAsync("/api/chores", new
        {
            title = " ",
            pointMultiplier = 0,
            steps = new object[] { new { title = "Wash", reminderDelayMinutes = 5 }, new { title = "Hang", repeatIntervalMinutes = 30 } },
        }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ValidationProblem>(Ct);
        Assert.Contains("required", body!.ErrorCodes["Title"]);
        Assert.Contains("chore.multiplier_out_of_range", body.ErrorCodes["PointMultiplier"]);
        Assert.Contains("step.first_has_delay", body.ErrorCodes["Steps[0].ReminderDelayMinutes"]);
        Assert.Contains("step.interval_without_delay", body.ErrorCodes["Steps[1].RepeatIntervalMinutes"]);
        Assert.Empty(await parent.LibraryAsync(Ct));
    }

    [Fact]
    public async Task A_child_cannot_change_the_library_or_see_the_archive()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var choreId = await factory.Services.SeedChoreAsync(seeded.Id);
        var child = await factory.AsAsync(seeded.Child);
        var chore = await child.GetChoreAsync(choreId, Ct);

        HttpResponseMessage[] responses =
        [
            await child.PostAsJsonAsync("/api/chores", ChoreApi.WashClothes(), Ct),
            await child.PutAsJsonAsync($"/api/chores/{choreId}", ChoreApi.EditOf(chore, title: "Mine"), Ct),
            await child.DeleteAsync($"/api/chores/{choreId}", Ct),
            await child.PostAsync($"/api/chores/{choreId}/restore", null, Ct),
            await child.GetAsync("/api/chores/archived", Ct),
        ];

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode));
        var library = await child.LibraryAsync(Ct);
        Assert.Equal(["Wash clothes"], library.Select(c => c.Title));
    }

    private sealed record ValidationProblem(Dictionary<string, string[]> ErrorCodes);
}
