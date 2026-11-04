using System.Net;
using Chores.Api.Tests.Infrastructure;
using Chores.Infrastructure.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Chores.Api.Tests.Members;

public class OperatorPasswordResetTests(PostgresApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<string?> ResetAsync(string username, string password)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<OperatorPasswordReset>().ResetAsync(username, password, Ct);
    }

    [Fact]
    public async Task An_operator_resets_a_parent_password_and_ends_their_sessions()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var session = await factory.SignInAsync(seeded.Parent.Username, seeded.Parent.Password);

        var error = await ResetAsync(seeded.Parent.Username.ToUpperInvariant(), "operator-chosen-pw");

        Assert.Null(error);
        await factory.SignInAsync(seeded.Parent.Username, "operator-chosen-pw");
        Assert.Equal(HttpStatusCode.BadRequest, (await factory.RefreshAsync(session.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task The_role_policy_still_applies() =>
        Assert.NotNull(await ResetAsync((await factory.Services.SeedFamilyAsync()).Parent.Username, "short"));

    [Fact]
    public async Task An_unknown_username_is_reported() =>
        Assert.NotNull(await ResetAsync(TestData.UniqueUsername(), "operator-chosen-pw"));
}
