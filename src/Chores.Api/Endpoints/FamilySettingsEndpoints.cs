using Chores.Api.Auth;
using Chores.Application.Features.FamilySettings;
using Mediator;

namespace Chores.Api.Endpoints;

internal static class FamilySettingsEndpoints
{
    public static IEndpointRouteBuilder MapFamilySettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var settings = app.MapGroup("/api/family/settings").RequireAuthorization(AuthSetup.ParentPolicy);

        settings.MapGet("", (IMediator mediator, CancellationToken cancellationToken) =>
            mediator.Send(new GetFamilySettingsQuery(), cancellationToken));

        settings.MapPut("", (UpdateFamilySettingsCommand command, IMediator mediator, CancellationToken cancellationToken) =>
            mediator.Send(command, cancellationToken));

        return app;
    }
}
