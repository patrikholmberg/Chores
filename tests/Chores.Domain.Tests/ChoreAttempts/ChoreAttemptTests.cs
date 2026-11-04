using Chores.Domain.ChoreAttempts;
using Chores.Domain.ChoreLibrary;
using Chores.Domain.Common;
using Chores.Domain.Families;

namespace Chores.Domain.Tests.ChoreAttempts;

public class ChoreAttemptTests
{
    private static readonly Guid FamilyId = Guid.NewGuid();
    private static readonly Guid ChildId = Guid.NewGuid();
    private static readonly Guid ParentId = Guid.NewGuid();
    private static readonly FamilyTimeZone Stockholm = FamilyTimeZone.Create("Europe/Stockholm");
    private static readonly TimeSpan Week = TimeSpan.FromDays(7);

    // Monday 2 November 2026, 18:00 in Stockholm (CET).
    private static readonly DateTimeOffset Monday = new(2026, 11, 2, 17, 0, 0, TimeSpan.Zero);

    private static Chore WashClothes(int pointMultiplier = 1) => Chore.Create(
        Guid.NewGuid(),
        FamilyId,
        "Wash clothes",
        "Whites only",
        pointMultiplier,
        [
            new StepDefinition("Wash clothes", null, null, null),
            new StepDefinition("Hang clothes", "On the rack", TimeSpan.FromMinutes(90), TimeSpan.FromMinutes(30)),
            new StepDefinition("Collect clothes", null, TimeSpan.FromMinutes(2880), null),
        ]);

    private static ChoreAttempt StartAt(DateTimeOffset now, Chore? chore = null) =>
        ChoreAttempt.Start(Guid.NewGuid(), ChildId, chore ?? WashClothes(), Stockholm, Week, now);

    private static ChoreAttempt CompletedAttempt(DateTimeOffset completedAt, int pointMultiplier = 1)
    {
        var attempt = StartAt(Monday, WashClothes(pointMultiplier));
        attempt.CompleteStep(0, completedAt);
        attempt.CompleteStep(1, completedAt);
        attempt.CompleteStep(2, completedAt);
        return attempt;
    }

    private static void AssertViolation(string code, Action action) =>
        Assert.Equal(code, Assert.Throws<DomainRuleViolation>(action).Code);

    [Fact]
    public void Start_copies_the_chore()
    {
        var chore = WashClothes(pointMultiplier: 2);

        var attempt = StartAt(Monday, chore);

        Assert.Equal(FamilyId, attempt.FamilyId);
        Assert.Equal(ChildId, attempt.ChildId);
        Assert.Equal(chore.Id, attempt.ChoreId);
        Assert.Equal("Wash clothes", attempt.Title);
        Assert.Equal("Whites only", attempt.Description);
        Assert.Equal(2, attempt.PointMultiplier);
        Assert.Equal(["Wash clothes", "Hang clothes", "Collect clothes"], attempt.Steps.Select(s => s.Title));
        Assert.Equal([0, 1, 2], attempt.Steps.Select(s => s.Position));
        Assert.Equal("On the rack", attempt.Steps[1].Description);
        Assert.Null(attempt.Steps[0].ReminderDelay);
        Assert.Equal(TimeSpan.FromMinutes(90), attempt.Steps[1].ReminderDelay);
        Assert.Equal(TimeSpan.FromMinutes(30), attempt.Steps[1].RepeatInterval);
        Assert.Equal(TimeSpan.FromMinutes(2880), attempt.Steps[2].ReminderDelay);
        Assert.All(attempt.Steps, s => Assert.Equal(attempt.Id, s.AttemptId));
        Assert.All(attempt.Steps, s => Assert.Equal(FamilyId, s.FamilyId));
        Assert.All(attempt.Steps, s => Assert.False(s.IsDone));
        Assert.Equal(AttemptState.Open, attempt.StateAt(Monday));
        Assert.Equal(0, attempt.CurrentStep!.Position);
    }

    [Fact]
    public void Revising_the_chore_after_the_start_leaves_the_attempt_unchanged()
    {
        var chore = WashClothes();
        var attempt = StartAt(Monday, chore);

        chore.Revise("Laundry", null, 3, [new StepDefinition("Do it", null, null, null)]);
        chore.Archive(Monday);

        Assert.Equal("Wash clothes", attempt.Title);
        Assert.Equal(1, attempt.PointMultiplier);
        Assert.Equal(3, attempt.Steps.Count);
        Assert.Equal(TimeSpan.FromMinutes(90), attempt.Steps[1].ReminderDelay);
    }

    [Fact]
    public void An_archived_chore_cannot_be_started()
    {
        var chore = WashClothes();
        chore.Archive(Monday);

        AssertViolation(AttemptErrorCodes.ChoreArchived, () => StartAt(Monday, chore));
    }

    [Theory]
    // 23:59 in Stockholm is still Monday.
    [InlineData("2026-11-02T22:59:00Z", "2026-11-02")]
    // Midnight in Stockholm is Tuesday although it is still Monday in UTC.
    [InlineData("2026-11-02T23:00:00Z", "2026-11-03")]
    public void The_start_day_uses_the_family_time_zone(string startedAt, string expectedDay)
    {
        var attempt = StartAt(DateTimeOffset.Parse(startedAt, System.Globalization.CultureInfo.InvariantCulture));

        Assert.Equal(DateOnly.Parse(expectedDay, System.Globalization.CultureInfo.InvariantCulture), attempt.StartDay);
    }

    [Fact]
    public void An_open_attempt_is_abandoned_exactly_at_its_expiry()
    {
        var attempt = StartAt(Monday);

        Assert.Equal(Monday + Week, attempt.ExpiresAt);
        Assert.Equal(AttemptState.Open, attempt.StateAt(Monday + Week - TimeSpan.FromTicks(1)));
        Assert.Equal(AttemptState.Abandoned, attempt.StateAt(Monday + Week));
    }

    [Fact]
    public void Steps_are_completed_in_order_with_their_times()
    {
        var attempt = StartAt(Monday);
        var later = Monday.AddMinutes(45);

        attempt.CompleteStep(0, later);

        Assert.Equal(later, attempt.Steps[0].CompletedAt);
        Assert.Equal(1, attempt.CurrentStep!.Position);
        Assert.Equal(AttemptState.Open, attempt.StateAt(later));
        Assert.Null(attempt.CompletedAt);
    }

    [Fact]
    public void Completing_a_later_step_is_refused()
    {
        var attempt = StartAt(Monday);
        attempt.CompleteStep(0, Monday);

        AssertViolation(AttemptErrorCodes.NotCurrentStep, () => attempt.CompleteStep(2, Monday));
        Assert.Equal(1, attempt.CurrentStep!.Position);
    }

    [Fact]
    public void Completing_the_same_step_twice_is_refused()
    {
        var attempt = StartAt(Monday);
        attempt.CompleteStep(0, Monday);

        AssertViolation(AttemptErrorCodes.NotCurrentStep, () => attempt.CompleteStep(0, Monday));
        Assert.Equal(1, attempt.CurrentStep!.Position);
    }

    [Fact]
    public void The_last_step_completes_the_attempt_and_it_counts_toward_its_start_day()
    {
        var wednesday = Monday.AddDays(2);

        var attempt = CompletedAttempt(wednesday);

        Assert.Equal(wednesday, attempt.CompletedAt);
        Assert.Null(attempt.CurrentStep);
        Assert.Equal(AttemptState.Complete, attempt.StateAt(wednesday));
        Assert.Equal(new DateOnly(2026, 11, 2), attempt.StartDay);
    }

    [Fact]
    public void A_complete_attempt_is_never_abandoned()
    {
        var attempt = CompletedAttempt(Monday.AddDays(6));

        Assert.Equal(AttemptState.Complete, attempt.StateAt(Monday.AddDays(30)));
    }

    [Fact]
    public void Restart_clears_the_steps_but_keeps_the_identity_start_day_and_expiry()
    {
        var attempt = StartAt(Monday);
        attempt.CompleteStep(0, Monday);
        attempt.CompleteStep(1, Monday.AddHours(2));
        var id = attempt.Id;
        var tuesday = Monday.AddDays(1);

        attempt.Restart(tuesday);

        Assert.Equal(id, attempt.Id);
        Assert.Equal(new DateOnly(2026, 11, 2), attempt.StartDay);
        Assert.Equal(Monday + Week, attempt.ExpiresAt);
        Assert.Equal(0, attempt.CurrentStep!.Position);
        Assert.All(attempt.Steps, s => Assert.Null(s.CompletedAt));
        Assert.Equal(AttemptState.Open, attempt.StateAt(tuesday));
    }

    [Fact]
    public void A_complete_attempt_cannot_be_restarted_completed_or_cancelled()
    {
        var attempt = CompletedAttempt(Monday);

        AssertViolation(AttemptErrorCodes.NotOpen, () => attempt.Restart(Monday));
        AssertViolation(AttemptErrorCodes.NotOpen, () => attempt.CompleteStep(2, Monday));
        AssertViolation(AttemptErrorCodes.NotOpen, () => attempt.EnsureCancellable(Monday));
    }

    [Fact]
    public void An_abandoned_attempt_cannot_be_restarted_completed_or_cancelled()
    {
        var attempt = StartAt(Monday);
        var expired = Monday + Week;

        AssertViolation(AttemptErrorCodes.NotOpen, () => attempt.Restart(expired));
        AssertViolation(AttemptErrorCodes.NotOpen, () => attempt.CompleteStep(0, expired));
        AssertViolation(AttemptErrorCodes.NotOpen, () => attempt.EnsureCancellable(expired));
    }

    [Fact]
    public void An_open_attempt_can_be_cancelled() => StartAt(Monday).EnsureCancellable(Monday);

    [Fact]
    public void Reject_records_the_parent_time_and_reason()
    {
        var attempt = CompletedAttempt(Monday);
        var later = Monday.AddHours(1);

        attempt.Reject(ParentId, "  Not clean ", later);

        Assert.Equal(AttemptState.Rejected, attempt.StateAt(later));
        Assert.Equal(later, attempt.RejectedAt);
        Assert.Equal(ParentId, attempt.RejectedBy);
        Assert.Equal("Not clean", attempt.RejectionReason);
    }

    [Fact]
    public void A_blank_rejection_reason_is_stored_as_none()
    {
        var attempt = CompletedAttempt(Monday);

        attempt.Reject(ParentId, "   ", Monday);

        Assert.Null(attempt.RejectionReason);
    }

    [Fact]
    public void An_open_attempt_cannot_be_rejected() =>
        AssertViolation(AttemptErrorCodes.NotComplete, () => StartAt(Monday).Reject(ParentId, null, Monday));

    [Fact]
    public void A_rejected_attempt_cannot_be_rejected_again()
    {
        var attempt = CompletedAttempt(Monday);
        attempt.Reject(ParentId, "First", Monday);

        AssertViolation(AttemptErrorCodes.NotComplete, () => attempt.Reject(ParentId, "Second", Monday));
        Assert.Equal("First", attempt.RejectionReason);
    }

    [Fact]
    public void Rejection_after_the_expiry_is_refused()
    {
        var attempt = CompletedAttempt(Monday);

        AssertViolation(AttemptErrorCodes.RejectionWindowClosed, () => attempt.Reject(ParentId, null, Monday + Week));
        Assert.Null(attempt.RejectedAt);
    }

    [Fact]
    public void A_too_long_rejection_reason_is_refused() =>
        AssertViolation(
            AttemptErrorCodes.RejectionReasonTooLong,
            () => CompletedAttempt(Monday).Reject(ParentId, new string('x', ChoreAttempt.RejectionReasonMaxLength + 1), Monday));

    [Fact]
    public void Earned_points_are_the_multiplier_times_base_points_only_when_complete()
    {
        var open = StartAt(Monday);
        var complete = CompletedAttempt(Monday, pointMultiplier: 2);
        var rejected = CompletedAttempt(Monday, pointMultiplier: 2);
        rejected.Reject(ParentId, null, Monday);

        Assert.Equal(0, open.EarnedPoints(10, Monday));
        Assert.Equal(0, open.EarnedPoints(10, Monday + Week));
        Assert.Equal(20, complete.EarnedPoints(10, Monday));
        Assert.Equal(0, rejected.EarnedPoints(10, Monday));
    }

    [Fact]
    public void Every_change_increments_the_revision()
    {
        var attempt = StartAt(Monday);

        attempt.CompleteStep(0, Monday);
        attempt.Restart(Monday);

        Assert.Equal(2, attempt.Revision);
    }
}
