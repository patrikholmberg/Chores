using System.Text.Json;
using Chores.Api.Tests.Infrastructure;
using Chores.Application.Common.Identity;
using Ixnas.AltchaNet;
using Microsoft.Extensions.DependencyInjection;

namespace Chores.Api.Tests.Signup;

public class CaptchaTests(PostgresApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Challenge_has_the_fields_the_widget_expects()
    {
        var json = await factory.CreateClient().GetStringAsync("/api/captcha/challenge", Ct);
        var root = JsonDocument.Parse(json).RootElement;

        foreach (var field in new[] { "algorithm", "challenge", "salt", "signature", "maxnumber" })
        {
            Assert.True(root.TryGetProperty(field, out _), $"Missing '{field}' in {json}");
        }
    }

    [Fact]
    public async Task A_solution_is_accepted_once()
    {
        var solution = await factory.CreateClient().SolveCaptchaAsync();

        Assert.True(await VerifyAsync(solution));
        Assert.False(await VerifyAsync(solution));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("bm90LWEtc29sdXRpb24=")]
    public async Task Missing_or_garbage_solutions_are_rejected(string? solution) =>
        Assert.False(await VerifyAsync(solution));

    [Fact]
    public async Task An_expired_solution_is_rejected()
    {
        string solution;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var challenge = scope.ServiceProvider.GetRequiredService<AltchaService>()
                .Generate(configuration => configuration with { Expiry = AltchaExpiry.FromSeconds(1) });
            solution = CaptchaSolver.Solve(challenge);
        }

        await Task.Delay(TimeSpan.FromSeconds(2), Ct);

        Assert.False(await VerifyAsync(solution));
    }

    private async Task<bool> VerifyAsync(string? solution)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ICaptchaVerifier>().VerifyAsync(solution, Ct);
    }
}
