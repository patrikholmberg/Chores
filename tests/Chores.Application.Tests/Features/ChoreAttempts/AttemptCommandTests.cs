using Chores.Application.Common.Exceptions;
using Chores.Application.Common.Validation;
using Chores.Application.Features.ChoreAttempts;
using Chores.Domain.ChoreAttempts;
using Chores.Domain.Common;

namespace Chores.Application.Tests.Features.ChoreAttempts;

public class AttemptCommandTests
{
    private readonly AttemptWorld _world = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private StartAttemptHandler StartAs(Guid childId) => new(
        _world.Chores, _world.Attempts, _world.Families, _world.Child(childId), _world.UnitOfWork, _world.Time);

    private CompleteStepHandler CompleteAs(Guid childId) => new(
        _world.Attempts, _world.Families, _world.Child(childId), _world.UnitOfWork, _world.Time);

    private RestartAttemptHandler RestartAs(Guid childId) => new(
        _world.Attempts, _world.Families, _world.Child(childId), _world.UnitOfWork, _world.Time);

    private CancelAttemptHandler CancelAs(Common.FakeCurrentMember member) => new(_world.Attempts, member, _world.UnitOfWork, _world.Time);

    private RejectAttemptHandler RejectAsParent() => new(_world.Attempts, _world.Families, _world.Parent, _world.UnitOfWork, _world.Time);

    [Fact]
    public async Task Start_creates_an_open_attempt_for_the_child_and_saves()
    {
        var chore = _world.AddChore(steps: 3);

        var dto = await StartAs(_world.ChildA).Handle(new StartAttemptCommand(chore.Id), Ct);

        Assert.Equal(_world.ChildA, dto.ChildId);
        Assert.Equal("Nova", dto.ChildName);
        Assert.Equal(AttemptState.Open, dto.State);
        Assert.Equal(0, dto.CurrentStepPosition);
        Assert.Equal(new DateOnly(2026, 11, 2), dto.StartDay);
        Assert.Equal(AttemptWorld.Monday.AddDays(7), dto.ExpiresAt);
        Assert.Equal(3, dto.Steps.Count);
        Assert.Single(_world.Attempts.Attempts);
        Assert.Equal(1, _world.UnitOfWork.Saves);
    }

    [Fact]
    public async Task Start_uses_the_family_attempt_window()
    {
        _world.Family.ChangeSettings(10, 2);
        var chore = _world.AddChore();

        var dto = await StartAs(_world.ChildA).Handle(new StartAttemptCommand(chore.Id), Ct);

        Assert.Equal(AttemptWorld.Monday.AddDays(2), dto.ExpiresAt);
    }

    [Fact]
    public async Task Starting_an_archived_chore_is_not_found()
    {
        var chore = _world.AddChore();
        chore.Archive(AttemptWorld.Monday);

        await Assert.ThrowsAsync<NotFoundException>(async () =>
            await StartAs(_world.ChildA).Handle(new StartAttemptCommand(chore.Id), Ct));
        Assert.Empty(_world.Attempts.Attempts);
    }

    [Fact]
    public async Task Starting_an_unknown_chore_is_not_found() =>
        await Assert.ThrowsAsync<NotFoundException>(async () =>
            await StartAs(_world.ChildA).Handle(new StartAttemptCommand(Guid.NewGuid()), Ct));

    [Fact]
    public async Task Completing_steps_advances_and_completes_the_attempt()
    {
        var attempt = _world.AddAttempt(_world.ChildA, _world.AddChore(steps: 2));

        var afterFirst = await CompleteAs(_world.ChildA).Handle(new CompleteStepCommand(attempt.Id, 0), Ct);
        var afterLast = await CompleteAs(_world.ChildA).Handle(new CompleteStepCommand(attempt.Id, 1), Ct);

        Assert.Equal(1, afterFirst.CurrentStepPosition);
        Assert.Equal(AttemptState.Complete, afterLast.State);
        Assert.Null(afterLast.CurrentStepPosition);
        Assert.All(afterLast.Steps, s => Assert.Equal(AttemptWorld.Monday, s.CompletedAt));
    }

    [Fact]
    public async Task A_child_acting_on_a_siblings_attempt_gets_not_found()
    {
        var attempt = _world.AddAttempt(_world.ChildA, _world.AddChore(steps: 2));

        await Assert.ThrowsAsync<NotFoundException>(async () =>
            await CompleteAs(_world.ChildB).Handle(new CompleteStepCommand(attempt.Id, 0), Ct));
        await Assert.ThrowsAsync<NotFoundException>(async () =>
            await RestartAs(_world.ChildB).Handle(new RestartAttemptCommand(attempt.Id), Ct));
        await Assert.ThrowsAsync<NotFoundException>(async () =>
            await CancelAs(_world.Child(_world.ChildB)).Handle(new CancelAttemptCommand(attempt.Id), Ct));
        Assert.Equal(0, attempt.CurrentStep!.Position);
        Assert.Equal(0, _world.UnitOfWork.Saves);
    }

    [Fact]
    public async Task Restart_returns_to_the_first_step()
    {
        var attempt = _world.AddAttempt(_world.ChildA, _world.AddChore(steps: 3));
        attempt.CompleteStep(0, AttemptWorld.Monday);
        _world.Time.Now = AttemptWorld.Monday.AddDays(1);

        var dto = await RestartAs(_world.ChildA).Handle(new RestartAttemptCommand(attempt.Id), Ct);

        Assert.Equal(0, dto.CurrentStepPosition);
        Assert.Equal(new DateOnly(2026, 11, 2), dto.StartDay);
    }

    [Fact]
    public async Task The_owner_cancels_an_open_attempt()
    {
        var attempt = _world.AddAttempt(_world.ChildA, _world.AddChore());

        await CancelAs(_world.Child(_world.ChildA)).Handle(new CancelAttemptCommand(attempt.Id), Ct);

        Assert.Empty(_world.Attempts.Attempts);
        Assert.Equal(1, _world.UnitOfWork.Saves);
    }

    [Fact]
    public async Task A_parent_cancels_any_open_attempt()
    {
        var attempt = _world.AddAttempt(_world.ChildB, _world.AddChore());

        await CancelAs(_world.Parent).Handle(new CancelAttemptCommand(attempt.Id), Ct);

        Assert.Empty(_world.Attempts.Attempts);
    }

    [Fact]
    public async Task A_complete_attempt_cannot_be_cancelled()
    {
        var attempt = _world.AddAttempt(_world.ChildA, _world.AddChore(), complete: true);

        var error = await Assert.ThrowsAsync<DomainRuleViolation>(async () =>
            await CancelAs(_world.Child(_world.ChildA)).Handle(new CancelAttemptCommand(attempt.Id), Ct));

        Assert.Equal(AttemptErrorCodes.NotOpen, error.Code);
        Assert.Single(_world.Attempts.Attempts);
    }

    [Fact]
    public async Task Rejection_records_the_parent_and_reason()
    {
        var attempt = _world.AddAttempt(_world.ChildA, _world.AddChore(), complete: true);

        var dto = await RejectAsParent().Handle(new RejectAttemptCommand(attempt.Id, "Not clean"), Ct);

        Assert.Equal(AttemptState.Rejected, dto.State);
        Assert.NotNull(dto.Rejection);
        Assert.Equal(_world.ParentId, dto.Rejection.ByMemberId);
        Assert.Equal("alex", dto.Rejection.ByName);
        Assert.Equal("Not clean", dto.Rejection.Reason);
        Assert.Equal(AttemptWorld.Monday, dto.Rejection.At);
        Assert.Equal(1, _world.UnitOfWork.Saves);
    }

    [Fact]
    public void A_too_long_rejection_reason_is_reported_on_that_field()
    {
        var result = new RejectAttemptValidator().Validate(new RejectAttemptCommand(Guid.NewGuid(), new string('x', 501)));

        Assert.Contains(result.Errors, e => e.PropertyName == "Reason" && e.ErrorCode == ValidationCodes.RejectionReasonTooLong);
        Assert.True(new RejectAttemptValidator().Validate(new RejectAttemptCommand(Guid.NewGuid(), null)).IsValid);
    }
}
