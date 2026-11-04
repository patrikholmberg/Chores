using Chores.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Chores.Infrastructure.Identity;

/// <summary>Identity user store that only stages changes, so the unit of work saves credential and Member together.</summary>
internal sealed class MemberCredentialStore : UserOnlyStore<MemberCredential, ChoresDbContext, Guid>
{
    public MemberCredentialStore(ChoresDbContext context, IdentityErrorDescriber? describer = null)
        : base(context, describer)
    {
        AutoSaveChanges = false;
    }

    /// <summary>
    /// The base implementation re-attaches the user on every call, which resets the tracked original concurrency stamp,
    /// so several staged updates before one save would fail the optimistic concurrency check.
    /// </summary>
    public override Task<IdentityResult> UpdateAsync(MemberCredential user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        if (Context.Entry(user).State == EntityState.Detached)
        {
            Context.Attach(user);
        }

        user.ConcurrencyStamp = Guid.NewGuid().ToString();
        return Task.FromResult(IdentityResult.Success);
    }
}
