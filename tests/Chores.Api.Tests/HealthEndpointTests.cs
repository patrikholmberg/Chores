using System.Net;

namespace Chores.Api.Tests;

public class HealthEndpointTests(UnreachableDatabaseFactory factory) : IClassFixture<UnreachableDatabaseFactory>
{
    [Fact]
    public async Task Alive_is_healthy_even_when_database_is_down()
    {
        var response = await factory.CreateClient().GetAsync("/alive", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_is_unavailable_when_database_is_down()
    {
        var response = await factory.CreateClient().GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Unhealthy_response_discloses_only_the_status()
    {
        var response = await factory.CreateClient().GetAsync("/health", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Unhealthy", body);
    }
}
