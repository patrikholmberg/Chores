using Chores.Application.Features.ServerInfo;
using Mediator;

namespace Chores.Api.Endpoints;

internal static class ServerInfoEndpoints
{
    public static IEndpointRouteBuilder MapServerInfoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/info", async (IMediator mediator, CancellationToken cancellationToken) =>
            await mediator.Send(new GetServerInfoQuery(), cancellationToken));

        return app;
    }
}
