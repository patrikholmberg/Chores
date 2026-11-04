using Chores.Application.Common.Exceptions;
using Chores.Application.Features.ChoreAttempts;
using Chores.Application.Tests.Common;
using Chores.Domain.ChoreAttempts;

namespace Chores.Application.Tests.Features.ChoreAttempts;

public class AttemptQueryTests
{
    private readonly AttemptWorld _world = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private Task<IReadOnlyList<AttemptSummaryDto>> ListAs(FakeCurrentMember member) =>
        new ListAttemptsHandler(_world.Attempts, _world.Families, member, _world.Time).Handle(new ListAttemptsQuery(), Ct).AsTask();

    private Task<TodayPointsDto> TodayAs(FakeCurrentMember member) =>
        new GetTodayPointsHandler(_world.Attempts, _world.Families, member, _world.Time).Handle(new GetTodayPointsQuery(), Ct).AsTask();

    [Fact]
    public async Task A_child_lists_only_their_own_attempts_newest_first()
    {
        var chore = _world.AddChore();
        var older = _world.AddAttempt(_world.ChildA, chore, AttemptWorld.Monday.AddHours(-1), complete: true);
        var newer = _world.AddAttempt(_world.ChildA, chore);
        _world.AddAttempt(_world.ChildB, chore);

        var list = await ListAs(_world.Child(_world.ChildA));

        Assert.Equal([newer.Id, older.Id], list.Select(a => a.Id));
        Assert.Equal([AttemptState.Open, AttemptState.Complete], list.Select(a => a.State));
        Assert.All(list, a => Assert.Equal("Nova", a.ChildName));
    }

    [Fact]
    public async Task A_parent_lists_every_childs_attempts()
    {
        var chore = _world.AddChore();
        _world.AddAttempt(_world.ChildA, chore);
        _world.AddAttempt(_world.ChildA, chore, complete: true);
        _world.AddAttempt(_world.ChildB, chore);

        var list = await ListAs(_world.Parent);

        Assert.Equal(3, list.Count);
        Assert.Equal(["Liam", "Nova", "Nova"], list.Select(a => a.ChildName).Order());
    }

    [Fact]
    public async Task The_list_starts_window_minus_one_family_days_back()
    {
        var chore = _world.AddChore();
        // With a 7-day window, today (Monday 2 Nov) and the six days before it are listed: from Tuesday 27 Oct.
        var included = _world.AddAttempt(_world.ChildA, chore, new DateTimeOffset(2026, 10, 26, 23, 30, 0, TimeSpan.Zero));
        _world.AddAttempt(_world.ChildA, chore, new DateTimeOffset(2026, 10, 26, 22, 30, 0, TimeSpan.Zero));

        var list = await ListAs(_world.Child(_world.ChildA));

        Assert.Equal([included.Id], list.Select(a => a.Id));
        Assert.Equal(new DateOnly(2026, 10, 27), list[0].StartDay);
        Assert.Equal(AttemptState.Open, list[0].State);
    }

    [Fact]
    public async Task Today_counts_complete_attempts_started_today_times_base_points()
    {
        _world.AddAttempt(_world.ChildA, _world.AddChore("Empty the dishwasher", pointMultiplier: 2), complete: true);
        _world.AddAttempt(_world.ChildA, _world.AddChore("Wash clothes"), complete: true);
        _world.AddAttempt(_world.ChildA, _world.AddChore("Clean the kitchen", pointMultiplier: 5));

        var today = await TodayAs(_world.Child(_world.ChildA));

        Assert.Equal(new DateOnly(2026, 11, 2), today.Day);
        Assert.Equal(10, today.BasePoints);
        var only = Assert.Single(today.Children);
        Assert.Equal(_world.ChildA, only.ChildId);
        Assert.Equal(30, only.Points);
    }

    [Fact]
    public async Task Today_excludes_rejected_attempts_and_attempts_started_yesterday()
    {
        var dishwasher = _world.AddAttempt(_world.ChildA, _world.AddChore(pointMultiplier: 2), complete: true);
        dishwasher.Reject(_world.ParentId, null, AttemptWorld.Monday);
        var yesterday = _world.AddAttempt(_world.ChildA, _world.AddChore(), AttemptWorld.Monday.AddDays(-1));
        yesterday.CompleteStep(0, AttemptWorld.Monday);
        _world.AddAttempt(_world.ChildA, _world.AddChore(), complete: true);

        var today = await TodayAs(_world.Child(_world.ChildA));

        Assert.Equal(10, Assert.Single(today.Children).Points);
    }

    [Fact]
    public async Task Today_uses_the_current_base_points()
    {
        _world.AddAttempt(_world.ChildA, _world.AddChore(pointMultiplier: 2), complete: true);
        _world.Family.ChangeSettings(5, 7);

        var today = await TodayAs(_world.Child(_world.ChildA));

        Assert.Equal(10, Assert.Single(today.Children).Points);
    }

    [Fact]
    public async Task A_parent_sees_every_child_including_those_with_zero()
    {
        _world.AddAttempt(_world.ChildA, _world.AddChore(), complete: true);

        var today = await TodayAs(_world.Parent);

        Assert.Equal([("Liam", 0), ("Nova", 10)], today.Children.Select(c => (c.ChildName, c.Points)));
    }

    [Fact]
    public async Task Getting_a_siblings_attempt_is_not_found_but_a_parent_gets_it()
    {
        var attempt = _world.AddAttempt(_world.ChildA, _world.AddChore());

        await Assert.ThrowsAsync<NotFoundException>(async () =>
            await new GetAttemptHandler(_world.Attempts, _world.Families, _world.Child(_world.ChildB), _world.Time)
                .Handle(new GetAttemptQuery(attempt.Id), Ct));
        var dto = await new GetAttemptHandler(_world.Attempts, _world.Families, _world.Parent, _world.Time)
            .Handle(new GetAttemptQuery(attempt.Id), Ct);

        Assert.Equal(attempt.Id, dto.Id);
    }
}
