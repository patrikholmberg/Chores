using Chores.Application.Common.Identity;
using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Domain.Families;

namespace Chores.Infrastructure.Identity;

/// <summary>
/// Lets an operator with database access set any Member's password, until Parents can reset their own by email.
/// Applies the same policy, stamp change and session revocation as a Parent-initiated reset.
/// </summary>
public sealed class OperatorPasswordReset(
    MemberDirectory directory,
    TenantContext tenant,
    IMemberCredentials credentials,
    IUnitOfWork unitOfWork)
{
    public async Task<string?> ResetAsync(string username, string password, CancellationToken cancellationToken)
    {
        if (!Username.TryCreate(username, out var name)
            || await directory.FindByUsernameAsync(name, cancellationToken) is not { } member)
        {
            return $"No Member is named '{username}'.";
        }

        if (!PasswordPolicy.IsSatisfiedBy(member.Role, password))
        {
            return $"A {member.Role} password must be at least {PasswordPolicy.MinimumLength(member.Role)} characters.";
        }

        using (tenant.BeginScope(member.FamilyId))
        {
            await credentials.SetPasswordAsync(member.Id, password, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await credentials.RevokeSessionsAsync(member.Id, cancellationToken);
        return null;
    }
}
