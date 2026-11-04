namespace Chores.Application.Common.Tenancy;

/// <summary>
/// The Family whose data the current scope may read and change. It is the signed-in Member's Family unless an explicit
/// scope is open, which signup and operator tools use to act for a Family without a signed-in Member.
/// </summary>
public interface ITenantContext
{
    Guid? FamilyId { get; }
}

public sealed class TenantContext(ICurrentMember currentMember) : ITenantContext
{
    private Guid? _scopedFamilyId;

    public Guid? FamilyId => _scopedFamilyId ?? currentMember.FamilyId;

    /// <summary>Acts for <paramref name="familyId"/> until the returned scope is disposed.</summary>
    public IDisposable BeginScope(Guid familyId)
    {
        if (_scopedFamilyId is not null)
        {
            throw new InvalidOperationException("A tenant scope is already open.");
        }

        _scopedFamilyId = familyId;
        return new Scope(this);
    }

    private sealed class Scope(TenantContext owner) : IDisposable
    {
        public void Dispose() => owner._scopedFamilyId = null;
    }
}
