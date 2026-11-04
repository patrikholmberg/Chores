using Chores.Api.Auth;
using Chores.Application.Features.Account;
using Mediator;

namespace Chores.Api.Endpoints;

internal static class MeEndpoints
{
    public static IEndpointRouteBuilder MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        var me = app.MapGroup("/api/me").RequireAuthorization(AuthSetup.MemberPolicy);

        me.MapGet("", (IMediator mediator, CancellationToken cancellationToken) =>
            mediator.Send(new GetCurrentMemberQuery(), cancellationToken));

        me.MapPut("/language", async (SetOwnLanguageCommand command, IMediator mediator, CancellationToken cancellationToken) =>
        {
            await mediator.Send(command, cancellationToken);
            return Results.NoContent();
        });

        me.MapPut("/password", async (ChangeOwnPasswordCommand command, IMediator mediator, CancellationToken cancellationToken) =>
        {
            await mediator.Send(command, cancellationToken);
            return Results.NoContent();
        });

        return app;
    }
}
