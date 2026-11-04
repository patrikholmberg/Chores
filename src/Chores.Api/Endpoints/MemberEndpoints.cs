using Chores.Api.Auth;
using Chores.Application.Features.Members;
using Chores.Domain.Families;
using Mediator;

namespace Chores.Api.Endpoints;

internal static class MemberEndpoints
{
    public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var members = app.MapGroup("/api/members").RequireAuthorization(AuthSetup.ParentPolicy);

        members.MapGet("", (IMediator mediator, CancellationToken cancellationToken) =>
            mediator.Send(new ListMembersQuery(), cancellationToken));

        members.MapGet("/{id:guid}", (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
            mediator.Send(new GetMemberQuery(id), cancellationToken));

        members.MapPost("", async (AddMemberCommand command, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var member = await mediator.Send(command, cancellationToken);
            return Results.Created($"/api/members/{member.Id}", member);
        });

        members.MapPut("/{id:guid}", (Guid id, UpdateMemberRequest request, IMediator mediator, CancellationToken cancellationToken) =>
            mediator.Send(
                new UpdateMemberCommand(id, request.Username, request.DisplayName, request.Role, request.Email, request.Language, request.Password),
                cancellationToken));

        members.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            await mediator.Send(new RemoveMemberCommand(id), cancellationToken);
            return Results.NoContent();
        });

        members.MapPut("/{id:guid}/password", async (Guid id, PasswordRequest request, IMediator mediator, CancellationToken cancellationToken) =>
        {
            await mediator.Send(new SetMemberPasswordCommand(id, request.Password), cancellationToken);
            return Results.NoContent();
        });

        return app;
    }

    internal sealed record PasswordRequest(string Password);

    internal sealed record UpdateMemberRequest(
        string Username,
        string? DisplayName,
        Role Role,
        string? Email,
        string Language,
        string? Password);
}
