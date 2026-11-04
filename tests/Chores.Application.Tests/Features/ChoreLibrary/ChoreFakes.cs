using Chores.Application.Common.Persistence;
using Chores.Domain.ChoreLibrary;
using Chores.Domain.Families;

namespace Chores.Application.Tests.Features.ChoreLibrary;

internal sealed class FakeChoreRepository(params Chore[] chores) : IChoreRepository
{
    private readonly List<Chore> _chores = [.. chores];

    public Task<Chore?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_chores.SingleOrDefault(c => c.Id == id));

    public Task<IReadOnlyList<Chore>> ListAsync(bool archived, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Chore>>(_chores.Where(c => c.IsArchived == archived).ToList());

    public void Add(Chore chore) => _chores.Add(chore);
}

internal sealed class FakeFamilyRepository(Family? family) : IFamilyRepository
{
    public Task<Family?> GetCurrentAsync(CancellationToken cancellationToken) => Task.FromResult(family);

    public void Add(Family family) => throw new NotSupportedException();
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int Saves { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        Saves++;
        return Task.CompletedTask;
    }
}

internal static class ChoreTestData
{
    public static readonly Guid FamilyId = Guid.NewGuid();

    public static Chore NewChore(string title = "Wash clothes") =>
        Chore.Create(Guid.NewGuid(), FamilyId, title, null, 1, [new StepDefinition("Do it", null, null, null)]);

    /// <summary>A Family whose only Parent speaks <paramref name="language"/>.</summary>
    public static (Family Family, Guid ParentId) FamilyWithParent(PreferredLanguage language)
    {
        var parentId = Guid.NewGuid();
        var family = Family.Register(
            FamilyId, "Test", FamilyTimeZone.Create("Europe/Stockholm"), parentId, Username.Create("alex"), null, "alex@example.com", language);
        return (family, parentId);
    }
}
