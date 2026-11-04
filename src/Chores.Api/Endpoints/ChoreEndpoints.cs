using Chores.Api.Auth;
using Chores.Application.Features.ChoreLibrary;
using Mediator;

namespace Chores.Api.Endpoints;

internal static class ChoreEndpoints
{
    public static IEndpointRouteBuilder MapChoreEndpoints(this IEndpointRouteBuilder app)
    {
        // Every Member reads the library; only Parents change it or see the archive.
        var chores = app.MapGroup("/api/chores").RequireAuthorization(AuthSetup.MemberPolicy);

        chores.MapGet("", (IMediator mediator, CancellationToken cancellationToken) =>
            mediator.Send(new ListChoresQuery(), cancellationToken));

        chores.MapGet("/archived", (IMediator mediator, CancellationToken cancellationToken) =>
                mediator.Send(new ListArchivedChoresQuery(), cancellationToken))
            .RequireAuthorization(AuthSetup.ParentPolicy);

        chores.MapGet("/{id:guid}", (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
            mediator.Send(new GetChoreQuery(id), cancellationToken));

        chores.MapPost("", async (CreateChoreCommand command, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var chore = await mediator.Send(command, cancellationToken);
                return Results.Created($"/api/chores/{chore.Id}", chore);
            })
            .RequireAuthorization(AuthSetup.ParentPolicy);

        chores.MapPut("/{id:guid}", (Guid id, UpdateChoreRequest request, IMediator mediator, CancellationToken cancellationToken) =>
                mediator.Send(
                    new UpdateChoreCommand(id, request.Revision, request.Title, request.Description, request.PointMultiplier, request.Steps),
                    cancellationToken))
            .RequireAuthorization(AuthSetup.ParentPolicy);

        chores.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
            {
                await mediator.Send(new ArchiveChoreCommand(id), cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(AuthSetup.ParentPolicy);

        chores.MapPost("/{id:guid}/restore", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
            {
                await mediator.Send(new RestoreChoreCommand(id), cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(AuthSetup.ParentPolicy);

        return app;
    }

    internal sealed record UpdateChoreRequest(
        int Revision,
        string Title,
        string? Description,
        int? PointMultiplier,
        IReadOnlyList<StepInput> Steps);
}
