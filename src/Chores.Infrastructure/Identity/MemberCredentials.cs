using Chores.Application.Common.Exceptions;
using Chores.Application.Common.Identity;
using Chores.Domain.Families;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace Chores.Infrastructure.Identity;

internal sealed class MemberCredentials(UserManager<MemberCredential> users, IOpenIddictTokenManager tokens) : IMemberCredentials
{
    public Task<bool> IsUsernameTakenAsync(Username username, Guid? exceptMemberId, CancellationToken cancellationToken)
    {
        var normalized = users.NormalizeName(username.Value);
        return users.Users.AnyAsync(c => c.NormalizedUserName == normalized && c.Id != exceptMemberId, cancellationToken);
    }

    public async Task CreateAsync(Guid memberId, Username username, string? email, string password, CancellationToken cancellationToken)
    {
        var credential = new MemberCredential { Id = memberId, UserName = username.Value, Email = email };
        Ensure(await users.CreateAsync(credential, password));
    }

    public async Task UpdateAsync(Guid memberId, Username username, string? email, CancellationToken cancellationToken)
    {
        // Set directly rather than through SetUserNameAsync, which would also change the security stamp and sign the Member out.
        var credential = await FindAsync(memberId);
        credential.UserName = username.Value;
        credential.Email = email;
        await users.UpdateNormalizedUserNameAsync(credential);
        await users.UpdateNormalizedEmailAsync(credential);
        Ensure(await users.UpdateAsync(credential));
    }

    public async Task SetPasswordAsync(Guid memberId, string password, CancellationToken cancellationToken)
    {
        var credential = await FindAsync(memberId);
        if (await users.HasPasswordAsync(credential))
        {
            Ensure(await users.RemovePasswordAsync(credential));
        }

        Ensure(await users.AddPasswordAsync(credential, password));
        Ensure(await users.SetLockoutEndDateAsync(credential, null));
        Ensure(await users.ResetAccessFailedCountAsync(credential));
    }

    public async Task<bool> CheckPasswordAsync(Guid memberId, string password, CancellationToken cancellationToken) =>
        await users.CheckPasswordAsync(await FindAsync(memberId), password);

    public async Task DeleteAsync(Guid memberId, CancellationToken cancellationToken) =>
        Ensure(await users.DeleteAsync(await FindAsync(memberId)));

    public async Task RevokeSessionsAsync(Guid memberId, CancellationToken cancellationToken) =>
        await tokens.RevokeBySubjectAsync(memberId.ToString(), cancellationToken);

    private async Task<MemberCredential> FindAsync(Guid memberId) =>
        await users.FindByIdAsync(memberId.ToString()) ?? throw new NotFoundException("Member", memberId);

    /// <summary>Turns Identity failures into the same validation errors the Application validators produce.</summary>
    private static void Ensure(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.DuplicateUserName)))
        {
            throw new UsernameTakenException();
        }

        throw new ValidationException(result.Errors.Select(e => new ValidationFailure(
            e.Code.StartsWith("Password", StringComparison.Ordinal) ? "Password"
            : e.Code.Contains("UserName", StringComparison.Ordinal) ? "Username"
            : e.Code.Contains("Email", StringComparison.Ordinal) ? "Email"
            : string.Empty,
            e.Description)));
    }
}
