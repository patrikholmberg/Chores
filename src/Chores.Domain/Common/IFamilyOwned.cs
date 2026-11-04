namespace Chores.Domain.Common;

/// <summary>Marks an entity that belongs to exactly one Family. Persistence isolates these by the signed-in Member's Family.</summary>
public interface IFamilyOwned
{
    Guid FamilyId { get; }
}
