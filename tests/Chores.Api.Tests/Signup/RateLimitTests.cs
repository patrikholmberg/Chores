using System.Net;
using System.Net.Http.Json;
using Chores.Api.Tests.Infrastructure;
using Chores.Application.Common.Identity;
using Chores.Domain.Families;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Chores.Api.Tests.Signup;

public class RateLimitTests(PostgresApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Signups_over_the_limit_get_429_and_create_nothing()
    {
        await using var limited = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("RateLimiting:Signup:PermitLimit", "2"));
        var client = limited.CreateClient();

        for (var i = 0; i < 2; i++)
        {
            await client.PostAsJsonAsync("/api/families", new { }, Ct);
        }

        var username = TestData.UniqueUsername();
        var response = await client.PostAsJsonAsync("/api/families", new
        {
            familyName = "Too many",
            timeZone = "Europe/Stockholm",
            username,
            password = "long-enough-pw",
            email = "x@example.com",
            language = "en",
            captcha = await client.SolveCaptchaAsync(),
        }, Ct);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        await using var scope = limited.Services.CreateAsyncScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<IMemberCredentials>()
            .IsUsernameTakenAsync(Username.Create(username), null, Ct));
    }
}
