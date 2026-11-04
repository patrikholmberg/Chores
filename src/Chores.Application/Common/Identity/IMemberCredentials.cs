using Chores.Domain.Families;

namespace Chores.Application.Common.Identity;

/// <summary>
/// The login side of a Member: username, email and password hash, keyed by the Member's id. Changes are staged and
/// committed by <see cref="Persistence.IUnitOfWork"/> together with the Member.
/// </summary>
public interface IMemberCredentials
{
    /// <summary>True when any Member of any Family, other than <paramref name="exceptMemberId"/>, has the username.</summary>
    Task<bool> IsUsernameTakenAsync(Username username, Guid? exceptMemberId, CancellationToken cancellationToken);

    Task CreateAsync(Guid memberId, Username username, string? email, string password, CancellationToken cancellationToken);

    Task UpdateAsync(Guid memberId, Username username, string? email, CancellationToken cancellationToken);

    /// <summary>Sets a new password and invalidates the Member's other sessions at their next refresh.</summary>
    Task SetPasswordAsync(Guid memberId, string password, CancellationToken cancellationToken);

    Task<bool> CheckPasswordAsync(Guid memberId, string password, CancellationToken cancellationToken);

    /// <summary>Deletes the credential, so the Member can no longer sign in and the username becomes free.</summary>
    Task DeleteAsync(Guid memberId, CancellationToken cancellationToken);

    /// <summary>Revokes every issued token of the Member. Call after the change that requires it has been saved.</summary>
    Task RevokeSessionsAsync(Guid memberId, CancellationToken cancellationToken);
}
