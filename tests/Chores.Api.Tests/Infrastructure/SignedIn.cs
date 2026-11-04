namespace Chores.Api.Tests.Infrastructure;

public static class SignedIn
{
    /// <summary>A client calling the Api as <paramref name="member"/>.</summary>
    public static async Task<HttpClient> AsAsync(this PostgresApiFactory factory, SeededMember member) =>
        factory.CreateClientWithToken((await factory.SignInAsync(member.Username, member.Password)).AccessToken);
}
