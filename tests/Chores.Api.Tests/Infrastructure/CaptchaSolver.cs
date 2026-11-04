using System.Net.Http.Json;
using Ixnas.AltchaNet;

namespace Chores.Api.Tests.Infrastructure;

public static class CaptchaSolver
{
    /// <summary>Fetches a challenge from the Api and solves it like the browser widget would.</summary>
    public static async Task<string> SolveCaptchaAsync(this HttpClient client)
    {
        var challenge = await client.GetFromJsonAsync<AltchaChallenge>("/api/captcha/challenge", TestContext.Current.CancellationToken);
        return Solve(challenge!);
    }

    public static string Solve(AltchaChallenge challenge)
    {
        var result = Altcha.CreateSolver(new AltchaSolverConfiguration { IgnoreExpiry = true }).Solve(challenge);
        Assert.True(result.Success, result.Error?.Message);
        return result.Altcha;
    }
}
