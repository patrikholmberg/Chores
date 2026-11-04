using Chores.Application.Common.Tenancy;
using Chores.Application.Features.ServerInfo;
using Chores.Application.Tests.Common;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Chores.Application.Tests.Features;

public class GetServerInfoTests
{
    [Fact]
    public async Task Returns_application_version_through_the_mediator()
    {
        await using var provider = new ServiceCollection()
            .AddApplication()
            .AddScoped<ICurrentMember>(_ => new FakeCurrentMember())
            .BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var info = await mediator.Send(new GetServerInfoQuery(), TestContext.Current.CancellationToken);

        Assert.False(string.IsNullOrWhiteSpace(info.Version));
    }
}
