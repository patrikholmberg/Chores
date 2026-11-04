using System.Net.Http.Json;

namespace Chores.Api.Tests.ChoreLibrary;

public sealed record StepView(string Title, string? Description, int? ReminderDelayMinutes, int? RepeatIntervalMinutes);

public sealed record ChoreView(Guid Id, string Title, string? Description, int PointMultiplier, int Revision, bool Archived, StepView[] Steps);

public sealed record ChoreSummaryView(Guid Id, string Title, string? Description, int PointMultiplier, int StepCount);

internal static class ChoreApi
{
    /// <summary>The spec's "Wash clothes" example as a request body.</summary>
    public static object WashClothes(int? pointMultiplier = 1) => new
    {
        title = "Wash clothes",
        pointMultiplier,
        steps = new object[]
        {
            new { title = "Wash clothes" },
            new { title = "Hang clothes", reminderDelayMinutes = 90, repeatIntervalMinutes = 30 },
            new { title = "Collect clothes", reminderDelayMinutes = 2880 },
        },
    };

    public static async Task<ChoreView> CreateAsync(this HttpClient client, object body, CancellationToken ct)
    {
        var response = await client.PostAsJsonAsync("/api/chores", body, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ChoreView>(ct))!;
    }

    public static async Task<ChoreView> GetChoreAsync(this HttpClient client, Guid id, CancellationToken ct) =>
        (await client.GetFromJsonAsync<ChoreView>($"/api/chores/{id}", ct))!;

    public static async Task<ChoreSummaryView[]> LibraryAsync(this HttpClient client, CancellationToken ct) =>
        (await client.GetFromJsonAsync<ChoreSummaryView[]>("/api/chores", ct))!;

    /// <summary>An edit body that keeps <paramref name="chore"/> as it is, with the given changes.</summary>
    public static object EditOf(ChoreView chore, string? title = null, StepView[]? steps = null, int? revision = null) => new
    {
        revision = revision ?? chore.Revision,
        title = title ?? chore.Title,
        description = chore.Description,
        pointMultiplier = chore.PointMultiplier,
        steps = steps ?? chore.Steps,
    };
}
