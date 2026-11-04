using Chores.Application.Common.Persistence;
using Chores.Application.Tests.Common;
using Chores.Application.Tests.Features.ChoreLibrary;
using Chores.Domain.ChoreAttempts;
using Chores.Domain.ChoreLibrary;
using Chores.Domain.Families;

namespace Chores.Application.Tests.Features.ChoreAttempts;

internal sealed class FakeChoreAttemptRepository : IChoreAttemptRepository
{
    public List<ChoreAttempt> Attempts { get; } = [];

    public Task<ChoreAttempt?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Attempts.SingleOrDefault(a => a.Id == id));

    public Task<IReadOnlyList<ChoreAttempt>> ListAsync(Guid? childId, DateOnly fromDay, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ChoreAttempt>>(Attempts
            .Where(a => a.StartDay >= fromDay && (childId == null || a.ChildId == childId))
            .OrderByDescending(a => a.StartedAt)
            .ToList());

    public Task<IReadOnlyList<ChoreAttempt>> ListStartedOnAsync(DateOnly day, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ChoreAttempt>>(Attempts.Where(a => a.StartDay == day).ToList());

    public void Add(ChoreAttempt attempt) => Attempts.Add(attempt);

    public void Remove(ChoreAttempt attempt) => Attempts.Remove(attempt);
}

/// <summary>A Family in Stockholm with one Parent and two Children, its library and attempts, on a fixed clock.</summary>
internal sealed class AttemptWorld
{
    // Monday 2 November 2026, 18:00 in Stockholm.
    public static readonly DateTimeOffset Monday = new(2026, 11, 2, 17, 0, 0, TimeSpan.Zero);

    public AttemptWorld()
    {
        (Family, ParentId) = ChoreTestData.FamilyWithParent(PreferredLanguage.English);
        ChildA = Family.AddMember(Guid.NewGuid(), Username.Create("nova"), "Nova", Role.Child, null, PreferredLanguage.Swedish).Id;
        ChildB = Family.AddMember(Guid.NewGuid(), Username.Create("liam"), "Liam", Role.Child, null, PreferredLanguage.Swedish).Id;
        Chores = new FakeChoreRepository();
        Families = new FakeFamilyRepository(Family);
    }

    public Family Family { get; }

    public Guid ParentId { get; }

    public Guid ChildA { get; }

    public Guid ChildB { get; }

    public FakeChoreRepository Chores { get; }

    public FakeFamilyRepository Families { get; }

    public FakeChoreAttemptRepository Attempts { get; } = new();

    public FakeUnitOfWork UnitOfWork { get; } = new();

    public FixedTimeProvider Time { get; } = new(Monday);

    public FakeCurrentMember Parent => new(Role.Parent, Family.Id, ParentId);

    public FakeCurrentMember Child(Guid childId) => new(Role.Child, Family.Id, childId);

    public Chore AddChore(string title = "Wash clothes", int pointMultiplier = 1, int steps = 1)
    {
        var chore = Chore.Create(
            Guid.NewGuid(),
            Family.Id,
            title,
            null,
            pointMultiplier,
            Enumerable.Range(1, steps).Select(i => new StepDefinition($"Step {i}", null, null, null)).ToList());
        Chores.Add(chore);
        return chore;
    }

    /// <summary>An attempt started at <paramref name="startedAt"/> (the world's clock by default), optionally completed then.</summary>
    public ChoreAttempt AddAttempt(Guid childId, Chore chore, DateTimeOffset? startedAt = null, bool complete = false)
    {
        var at = startedAt ?? Time.Now;
        var attempt = ChoreAttempt.Start(Guid.NewGuid(), childId, chore, Family.TimeZone, Family.AttemptWindow, at);
        if (complete)
        {
            foreach (var step in attempt.Steps)
            {
                attempt.CompleteStep(step.Position, at);
            }
        }

        Attempts.Add(attempt);
        return attempt;
    }
}
