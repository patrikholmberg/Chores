using System.Reflection;
using Mediator;

namespace Chores.Application.Features.ServerInfo;

public sealed record GetServerInfoQuery : IQuery<ServerInfo>;

public sealed record ServerInfo(string Version);

public sealed class GetServerInfoHandler : IQueryHandler<GetServerInfoQuery, ServerInfo>
{
    public ValueTask<ServerInfo> Handle(GetServerInfoQuery query, CancellationToken cancellationToken)
    {
        var version = typeof(GetServerInfoHandler).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "unknown";

        return ValueTask.FromResult(new ServerInfo(version));
    }
}
