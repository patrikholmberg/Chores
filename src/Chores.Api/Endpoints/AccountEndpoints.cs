using Chores.Api.Infrastructure;
using Chores.Application.Common.Persistence;
using Chores.Domain.Families;
using Chores.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Chores.Api.Endpoints;

internal static class AccountEndpoints
{
    public const string SignInFailedCode = "signin.failed";

    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/account/login", LoginAsync)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimiting.Login);

        return app;
    }

    /// <summary>
    /// Checks the password (counting failures towards lockout) and sets the short-lived cookie that the authorize
    /// endpoint reads. Wrong password, unknown username and lockout all give the same answer.
    /// </summary>
    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        SignInManager<MemberCredential> signIn,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        if (Username.TryCreate(request.Username, out var username) && !string.IsNullOrEmpty(request.Password))
        {
            var result = await signIn.PasswordSignInAsync(username.Value, request.Password, isPersistent: false, lockoutOnFailure: true);

            // The credential store only stages changes: commit the failed-attempt count or its reset.
            await unitOfWork.SaveChangesAsync(cancellationToken);
            if (result.Succeeded)
            {
                return Results.NoContent();
            }
        }

        return Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "The username or password is incorrect.",
            extensions: new Dictionary<string, object?> { ["code"] = SignInFailedCode });
    }

    internal sealed record LoginRequest(string? Username, string? Password);
}
