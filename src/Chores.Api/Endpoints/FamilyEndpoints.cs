using Chores.Api.Infrastructure;
using Chores.Application.Features.Families;
using Mediator;

namespace Chores.Api.Endpoints;

internal static class FamilyEndpoints
{
    public static IEndpointRouteBuilder MapFamilyEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/usernames/{username}/availability", (string username, IMediator mediator, CancellationToken cancellationToken) =>
                mediator.Send(new CheckUsernameAvailabilityQuery(username), cancellationToken))
            .AllowAnonymous()
            .RequireRateLimiting(RateLimiting.UsernameCheck);

        app.MapPost("/api/families", async (RegisterFamilyCommand command, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return Results.Created("/api/me", result);
            })
            .AllowAnonymous()
            .RequireRateLimiting(RateLimiting.Signup);

        return app;
    }
}
