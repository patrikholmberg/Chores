using System.Text.Json;
using Chores.Api.Tests.Infrastructure;

namespace Chores.Api.Tests.SignIn;

public class DiscoveryTests(PostgresApiFactory factory)
{
    [Fact]
    public async Task Only_code_and_refresh_flows_with_S256_are_advertised()
    {
        var json = await factory.CreateClient().GetStringAsync("/.well-known/openid-configuration", TestContext.Current.CancellationToken);
        var root = JsonDocument.Parse(json).RootElement;

        Assert.Equal(["authorization_code", "refresh_token"], Strings(root, "grant_types_supported").Order());
        Assert.Equal(["code"], Strings(root, "response_types_supported"));
        Assert.Equal(["S256"], Strings(root, "code_challenge_methods_supported"));
        Assert.EndsWith("/connect/authorize", root.GetProperty("authorization_endpoint").GetString());
        Assert.EndsWith("/connect/token", root.GetProperty("token_endpoint").GetString());
        Assert.EndsWith("/connect/endsession", root.GetProperty("end_session_endpoint").GetString());
        Assert.EndsWith("/connect/revoke", root.GetProperty("revocation_endpoint").GetString());
    }

    private static string[] Strings(JsonElement root, string property) =>
        root.GetProperty(property).EnumerateArray().Select(e => e.GetString()!).ToArray();
}
