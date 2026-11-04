using System.Net.Http.Json;
using System.Text.Json;

namespace Chores.Api.Tests.ChoreAttempts;

public sealed record AttemptStepView(int Position, string Title, string? Description, int? ReminderDelayMinutes, int? RepeatIntervalMinutes, DateTimeOffset? CompletedAt);

public sealed record RejectionView(DateTimeOffset At, Guid ByMemberId, string ByName, string? Reason);

public sealed record AttemptView(
    Guid Id,
    Guid ChildId,
    string ChildName,
    Guid ChoreId,
    string Title,
    int PointMultiplier,
    DateOnly StartDay,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? CompletedAt,
    string State,
    int? CurrentStepPosition,
    RejectionView? Rejection,
    AttemptStepView[] Steps);

public sealed record AttemptSummaryView(Guid Id, Guid ChildId, string ChildName, string Title, DateOnly StartDay, string State, int? CurrentStepPosition);

public sealed record ChildPointsView(Guid ChildId, string ChildName, int Points);

public sealed record TodayPointsView(DateOnly Day, int BasePoints, ChildPointsView[] Children);

internal static class AttemptApi
{
    public static async Task<AttemptView> StartAsync(this HttpClient client, Guid choreId, CancellationToken ct)
    {
        var response = await client.PostAsJsonAsync("/api/attempts", new { choreId }, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AttemptView>(ct))!;
    }

    public static Task<HttpResponseMessage> CompleteStepAsync(this HttpClient client, Guid attemptId, int position, CancellationToken ct) =>
        client.PostAsync($"/api/attempts/{attemptId}/steps/{position}/complete", null, ct);

    public static async Task<AttemptView> CompleteAllAsync(this HttpClient client, AttemptView attempt, CancellationToken ct)
    {
        foreach (var step in attempt.Steps)
        {
            (await client.CompleteStepAsync(attempt.Id, step.Position, ct)).EnsureSuccessStatusCode();
        }

        return await client.GetAttemptAsync(attempt.Id, ct);
    }

    public static async Task<AttemptView> GetAttemptAsync(this HttpClient client, Guid id, CancellationToken ct) =>
        (await client.GetFromJsonAsync<AttemptView>($"/api/attempts/{id}", ct))!;

    public static async Task<AttemptSummaryView[]> AttemptsAsync(this HttpClient client, CancellationToken ct) =>
        (await client.GetFromJsonAsync<AttemptSummaryView[]>("/api/attempts", ct))!;

    public static async Task<TodayPointsView> TodayAsync(this HttpClient client, CancellationToken ct) =>
        (await client.GetFromJsonAsync<TodayPointsView>("/api/points/today", ct))!;

    public static Task<HttpResponseMessage> RejectAsync(this HttpClient client, Guid attemptId, string? reason, CancellationToken ct) =>
        client.PostAsJsonAsync($"/api/attempts/{attemptId}/reject", new { reason }, ct);

    public static async Task<string?> CodeOf(HttpResponseMessage response, CancellationToken ct) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(ct)).TryGetProperty("code", out var code) ? code.GetString() : null;
}
