using Chores.Api.Auth;
using Chores.Application.Features.ChoreAttempts;
using Mediator;

namespace Chores.Api.Endpoints;

internal static class AttemptEndpoints
{
    public static IEndpointRouteBuilder MapAttemptEndpoints(this IEndpointRouteBuilder app)
    {
        // Every Member reads attempts (a Child only their own); Children work them and Parents reject them.
        var attempts = app.MapGroup("/api/attempts").RequireAuthorization(AuthSetup.MemberPolicy);

        attempts.MapGet("", (IMediator mediator, CancellationToken cancellationToken) =>
            mediator.Send(new ListAttemptsQuery(), cancellationToken));

        attempts.MapGet("/{id:guid}", (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
            mediator.Send(new GetAttemptQuery(id), cancellationToken));

        attempts.MapPost("", async (StartAttemptCommand command, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var attempt = await mediator.Send(command, cancellationToken);
                return Results.Created($"/api/attempts/{attempt.Id}", attempt);
            })
            .RequireAuthorization(AuthSetup.ChildPolicy);

        attempts.MapPost("/{id:guid}/steps/{position:int}/complete", (Guid id, int position, IMediator mediator, CancellationToken cancellationToken) =>
                mediator.Send(new CompleteStepCommand(id, position), cancellationToken))
            .RequireAuthorization(AuthSetup.ChildPolicy);

        attempts.MapPost("/{id:guid}/restart", (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                mediator.Send(new RestartAttemptCommand(id), cancellationToken))
            .RequireAuthorization(AuthSetup.ChildPolicy);

        attempts.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            await mediator.Send(new CancelAttemptCommand(id), cancellationToken);
            return Results.NoContent();
        });

        attempts.MapPost("/{id:guid}/reject", (Guid id, RejectAttemptRequest? request, IMediator mediator, CancellationToken cancellationToken) =>
                mediator.Send(new RejectAttemptCommand(id, request?.Reason), cancellationToken))
            .RequireAuthorization(AuthSetup.ParentPolicy);

        app.MapGet("/api/points/today", (IMediator mediator, CancellationToken cancellationToken) =>
                mediator.Send(new GetTodayPointsQuery(), cancellationToken))
            .RequireAuthorization(AuthSetup.MemberPolicy);

        return app;
    }

    internal sealed record RejectAttemptRequest(string? Reason);
}
