using System.Net;
using System.Net.Http.Json;
using Chores.Api.Tests.Infrastructure;

namespace Chores.Api.Tests.Signup;

public class UsernameAvailabilityTests(PostgresApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_username_in_another_family_is_taken_regardless_of_case()
    {
        var seeded = await factory.Services.SeedFamilyAsync();

        var result = await factory.CreateClient()
            .GetFromJsonAsync<Availability>($"/api/usernames/{seeded.Child.Username.ToUpperInvariant()}/availability", Ct);

        Assert.False(result!.Available);
    }

    [Fact]
    public async Task A_free_username_is_available()
    {
        var result = await factory.CreateClient()
            .GetFromJsonAsync<Availability>($"/api/usernames/{TestData.UniqueUsername()}/availability", Ct);

        Assert.True(result!.Available);
    }

    [Fact]
    public async Task An_invalid_username_is_a_validation_error()
    {
        var response = await factory.CreateClient().GetAsync("/api/usernames/a%20b/availability", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("username.invalid", await response.Content.ReadAsStringAsync(Ct));
    }

    private sealed record Availability(bool Available);
}
