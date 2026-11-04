using System.Net;
using System.Net.Http.Json;
using Chores.Api.Tests.Infrastructure;

namespace Chores.Api.Tests.FamilySettings;

public class FamilySettingsTests(PostgresApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed record SettingsView(int BasePoints, int AttemptWindowDays);

    private sealed record ValidationProblem(Dictionary<string, string[]> ErrorCodes);

    [Fact]
    public async Task A_parent_reads_the_defaults_and_saves_new_values()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var parent = await factory.AsAsync(seeded.Parent);

        Assert.Equal(new SettingsView(10, 7), await parent.GetFromJsonAsync<SettingsView>("/api/family/settings", Ct));

        var response = await parent.PutAsJsonAsync("/api/family/settings", new { basePoints = 5, attemptWindowDays = 3 }, Ct);

        response.EnsureSuccessStatusCode();
        Assert.Equal(new SettingsView(5, 3), await response.Content.ReadFromJsonAsync<SettingsView>(Ct));
        Assert.Equal(new SettingsView(5, 3), await parent.GetFromJsonAsync<SettingsView>("/api/family/settings", Ct));
    }

    [Fact]
    public async Task Values_out_of_range_are_field_errors_and_change_nothing()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var parent = await factory.AsAsync(seeded.Parent);

        var response = await parent.PutAsJsonAsync("/api/family/settings", new { basePoints = 0, attemptWindowDays = 31 }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ValidationProblem>(Ct);
        Assert.Contains("family.base_points_out_of_range", body!.ErrorCodes["BasePoints"]);
        Assert.Contains("family.attempt_window_out_of_range", body.ErrorCodes["AttemptWindowDays"]);
        Assert.Equal(new SettingsView(10, 7), await parent.GetFromJsonAsync<SettingsView>("/api/family/settings", Ct));
    }

    [Fact]
    public async Task A_child_can_neither_read_nor_change_the_settings()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var child = await factory.AsAsync(seeded.Child);

        Assert.Equal(HttpStatusCode.Forbidden, (await child.GetAsync("/api/family/settings", Ct)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await child.PutAsJsonAsync("/api/family/settings", new { basePoints = 1, attemptWindowDays = 1 }, Ct)).StatusCode);

        var parent = await factory.AsAsync(seeded.Parent);
        Assert.Equal(new SettingsView(10, 7), await parent.GetFromJsonAsync<SettingsView>("/api/family/settings", Ct));
    }
}
