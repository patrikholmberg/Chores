using System.Net;
using System.Net.Http.Json;
using Chores.Api.Tests.Infrastructure;
using Chores.Application.Common.Identity;
using Chores.Domain.Families;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;

namespace Chores.Api.Tests.SignIn;

public class RefreshTests(PostgresApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_refresh_issues_new_tokens_and_the_old_refresh_token_cannot_be_reused()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var tokens = await factory.SignInAsync(seeded.Child.Username, seeded.Child.Password);

        var first = await factory.RefreshAsync(tokens.RefreshToken);
        var reused = await factory.RefreshAsync(tokens.RefreshToken);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var renewed = await first.Content.ReadFromJsonAsync<TokenResponse>(Ct);
        Assert.NotEqual(tokens.RefreshToken, renewed!.RefreshToken);
        Assert.Equal(HttpStatusCode.BadRequest, reused.StatusCode);
        Assert.Contains("invalid_grant", await reused.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task A_demoted_parent_gets_the_child_role_at_the_next_refresh()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var second = await factory.Services.AddParentAsync(seeded.Id);
        var tokens = await factory.SignInAsync(second.Username, second.Password);

        await factory.Services.ChangeFamilyAsync(seeded.Id, (family, _) =>
        {
            family.ChangeRole(second.Id, Role.Child, null);
            return Task.CompletedTask;
        });
        var refreshed = await (await factory.RefreshAsync(tokens.RefreshToken)).Content.ReadFromJsonAsync<TokenResponse>(Ct);

        var principal = await factory.ValidateAccessTokenAsync(refreshed!.AccessToken);
        Assert.Equal("child", principal.FindFirst("role")?.Value);
    }

    [Fact]
    public async Task A_changed_password_blocks_refresh()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var tokens = await factory.SignInAsync(seeded.Child.Username, seeded.Child.Password);

        await factory.Services.ChangeFamilyAsync(seeded.Id, (_, provider) =>
            provider.GetRequiredService<IMemberCredentials>().SetPasswordAsync(seeded.Child.Id, "efgh", CancellationToken.None));

        var response = await factory.RefreshAsync(tokens.RefreshToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("invalid_grant", await response.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task A_removed_member_cannot_refresh()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var tokens = await factory.SignInAsync(seeded.Child.Username, seeded.Child.Password);

        await factory.Services.ChangeFamilyAsync(seeded.Id, async (family, provider) =>
        {
            family.RemoveMember(seeded.Child.Id, DateTimeOffset.UtcNow);
            await provider.GetRequiredService<IMemberCredentials>().DeleteAsync(seeded.Child.Id, CancellationToken.None);
        });

        var response = await factory.RefreshAsync(tokens.RefreshToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Child, 90)]
    [InlineData(Role.Parent, 30)]
    public async Task The_session_lifetime_depends_on_the_role(Role role, int days)
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var member = role == Role.Child ? seeded.Child : seeded.Parent;
        var tokens = await factory.SignInAsync(member.Username, member.Password);

        // A refresh restarts the lifetime: the newest refresh token expires a full period from now.
        await factory.RefreshAsync(tokens.RefreshToken);

        await using var scope = factory.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictTokenManager>();
        var expirations = new List<DateTimeOffset>();
        var types = new List<string?>();
        await foreach (var token in manager.FindBySubjectAsync(member.Id.ToString(), Ct))
        {
            var type = await manager.GetTypeAsync(token, Ct);
            types.Add(type);
            if (type?.EndsWith("refresh_token", StringComparison.Ordinal) == true
                && await manager.GetExpirationDateAsync(token, Ct) is { } expiration)
            {
                expirations.Add(expiration);
            }
        }

        Assert.True(expirations.Count > 0, "Stored token types: " + string.Join(", ", types));
        var lifetime = expirations.Max() - DateTimeOffset.UtcNow;
        Assert.InRange(lifetime, TimeSpan.FromDays(days) - TimeSpan.FromMinutes(5), TimeSpan.FromDays(days));
    }
}
