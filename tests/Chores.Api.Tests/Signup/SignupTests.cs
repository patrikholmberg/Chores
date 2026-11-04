using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Chores.Api.Tests.Infrastructure;
using Chores.Application.Common.Identity;
using Chores.Infrastructure.Identity;
using Chores.Domain.Families;
using Microsoft.Extensions.DependencyInjection;

namespace Chores.Api.Tests.Signup;

public class SignupTests(PostgresApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static object Signup(string username, string? captcha, string? email = "alex@example.com") => new
    {
        familyName = "The Holmbergs",
        timeZone = "Europe/Stockholm",
        username,
        password = "long-enough-pw",
        email,
        language = "sv",
        captcha,
    };

    [Fact]
    public async Task Signup_creates_a_family_whose_parent_can_sign_in()
    {
        var client = factory.CreateClient();
        var username = TestData.UniqueUsername();

        var response = await client.PostAsJsonAsync("/api/families", Signup(username.ToUpperInvariant(), await client.SolveCaptchaAsync()), Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<Result>(Ct);

        await using var scope = factory.Services.CreateAsyncScope();
        var member = await scope.ServiceProvider.GetRequiredService<MemberDirectory>().FindAsync(result!.MemberId, Ct);
        Assert.NotNull(member);
        Assert.Equal(result.FamilyId, member.FamilyId);
        Assert.Equal(username, member.Username.Value);
        Assert.Equal(Role.Parent, member.Role);
        Assert.Equal("alex@example.com", member.Email);
        Assert.Equal(PreferredLanguage.Swedish, member.PreferredLanguage);
        Assert.True(await scope.ServiceProvider.GetRequiredService<IMemberCredentials>()
            .CheckPasswordAsync(member.Id, "long-enough-pw", Ct));
    }

    [Fact]
    public async Task Signup_without_captcha_is_rejected()
    {
        var username = TestData.UniqueUsername();

        var response = await factory.CreateClient().PostAsJsonAsync("/api/families", Signup(username, null), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("captcha.invalid", await response.Content.ReadAsStringAsync(Ct));
        Assert.False(await IsTakenAsync(username));
    }

    [Fact]
    public async Task Signup_with_a_reused_captcha_is_rejected()
    {
        var client = factory.CreateClient();
        var captcha = await client.SolveCaptchaAsync();
        await client.PostAsJsonAsync("/api/families", Signup(TestData.UniqueUsername(), captcha), Ct);
        var username = TestData.UniqueUsername();

        var response = await client.PostAsJsonAsync("/api/families", Signup(username, captcha), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(await IsTakenAsync(username));
    }

    [Fact]
    public async Task Signup_without_email_is_rejected_on_the_email_field()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/families", Signup(TestData.UniqueUsername(), await client.SolveCaptchaAsync(), email: null), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct)).RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("Email", out _));
    }

    [Fact]
    public async Task Two_concurrent_signups_with_one_username_create_one_family()
    {
        var client = factory.CreateClient();
        var username = TestData.UniqueUsername();
        var captchas = await Task.WhenAll(client.SolveCaptchaAsync(), client.SolveCaptchaAsync());

        var responses = await Task.WhenAll(captchas.Select(c => client.PostAsJsonAsync("/api/families", Signup(username, c), Ct)));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        var rejected = Assert.Single(responses, r => r.StatusCode == HttpStatusCode.BadRequest);
        Assert.Contains("username.taken", await rejected.Content.ReadAsStringAsync(Ct));
    }

    private async Task<bool> IsTakenAsync(string username)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IMemberCredentials>()
            .IsUsernameTakenAsync(Username.Create(username), null, Ct);
    }

    private sealed record Result(Guid FamilyId, Guid MemberId);
}
