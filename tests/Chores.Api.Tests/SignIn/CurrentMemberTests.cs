using System.Net.Http.Json;
using Chores.Api.Tests.Infrastructure;

namespace Chores.Api.Tests.SignIn;

public class CurrentMemberTests(PostgresApiFactory factory)
{
    [Fact]
    public async Task A_child_reads_their_profile_and_family()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var tokens = await factory.SignInAsync(seeded.Child.Username, seeded.Child.Password);

        var me = await factory.CreateClientWithToken(tokens.AccessToken)
            .GetFromJsonAsync<Profile>("/api/me", TestContext.Current.CancellationToken);

        Assert.Equal(new Profile(seeded.Child.Id, seeded.Child.Username, "Child", "child", "sv", seeded.Id, "Test family", "Europe/Stockholm"), me);
    }

    private sealed record Profile(
        Guid MemberId,
        string Username,
        string DisplayName,
        string Role,
        string PreferredLanguage,
        Guid FamilyId,
        string FamilyName,
        string TimeZone);
}
