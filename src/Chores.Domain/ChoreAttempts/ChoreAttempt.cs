using Chores.Domain.ChoreLibrary;
using Chores.Domain.Common;
using Chores.Domain.Families;

namespace Chores.Domain.ChoreAttempts;

/// <summary>
/// One Child's run through a snapshot of a Chore's Steps. It counts toward the Family Day it was started on, keeps that
/// day and its expiry time through a Restart, and is Abandoned when not complete by <see cref="ExpiresAt"/>.
/// </summary>
public sealed class ChoreAttempt : IFamilyOwned
{
    public const int RejectionReasonMaxLength = 500;

    private readonly List<AttemptStep> _steps = [];

    private ChoreAttempt(Guid id, Guid familyId, Guid childId, Chore chore, DateOnly startDay, DateTimeOffset startedAt, DateTimeOffset expiresAt)
    {
        Id = id;
        FamilyId = familyId;
        ChildId = childId;
        ChoreId = chore.Id;
        Title = chore.Title;
        Description = chore.Description;
        PointMultiplier = chore.PointMultiplier;
        StartDay = startDay;
        StartedAt = startedAt;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; }

    public Guid FamilyId { get; private set; }

    /// <summary>The Child who started the attempt and the only Member who works it.</summary>
    public Guid ChildId { get; private set; }

    /// <summary>The library Chore this attempt was started from. Kept for reference only; the attempt never reads it.</summary>
    public Guid ChoreId { get; private set; }

    public string Title { get; private set; }

    public string? Description { get; private set; }

    public int PointMultiplier { get; private set; }

    /// <summary>The Family Day the attempt counts toward, whenever it completes.</summary>
    public DateOnly StartDay { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>The start time plus the Family's attempt window in effect at the start.</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset? RejectedAt { get; private set; }

    /// <summary>The Parent who rejected the attempt.</summary>
    public Guid? RejectedBy { get; private set; }

    public string? RejectionReason { get; private set; }

    /// <summary>Incremented by every change, so two requests acting on the same attempt at once conflict.</summary>
    public int Revision { get; private set; }

    /// <summary>The snapshotted Steps in the order they are done.</summary>
    public IReadOnlyList<AttemptStep> Steps => _steps.OrderBy(s => s.Position).ToList();

    /// <summary>The first Step that is not done, or null once the attempt is complete.</summary>
    public AttemptStep? CurrentStep => Steps.FirstOrDefault(s => !s.IsDone);

    /// <summary>Starts an attempt of <paramref name="chore"/> for a Child, copying the Chore so later edits don't affect it.</summary>
    public static ChoreAttempt Start(
        Guid id,
        Guid childId,
        Chore chore,
        FamilyTimeZone timeZone,
        TimeSpan attemptWindow,
        DateTimeOffset now)
    {
        if (chore.IsArchived)
        {
            throw new DomainRuleViolation(AttemptErrorCodes.ChoreArchived, "An archived Chore cannot be started.");
        }

        var attempt = new ChoreAttempt(id, chore.FamilyId, childId, chore, timeZone.DayOf(now), now, now + attemptWindow);
        attempt._steps.AddRange(chore.Steps.Select(step => AttemptStep.CopyOf(chore.FamilyId, id, step)));
        return attempt;
    }

    public AttemptState StateAt(DateTimeOffset now) =>
        RejectedAt is not null ? AttemptState.Rejected
        : CompletedAt is not null ? AttemptState.Complete
        : now >= ExpiresAt ? AttemptState.Abandoned
        : AttemptState.Open;

    /// <summary>Point Multiplier × <paramref name="basePoints"/> for a complete attempt, otherwise 0.</summary>
    public int EarnedPoints(int basePoints, DateTimeOffset now) =>
        StateAt(now) == AttemptState.Complete ? PointMultiplier * basePoints : 0;

    /// <summary>Completes the current Step, which must be the one at <paramref name="position"/>. The last Step completes the attempt.</summary>
    public void CompleteStep(int position, DateTimeOffset now)
    {
        EnsureOpen(now);

        var current = CurrentStep!;
        if (current.Position != position)
        {
            throw new DomainRuleViolation(
                AttemptErrorCodes.NotCurrentStep,
                $"Step {position} is not the current Step; Step {current.Position} is.");
        }

        current.Complete(now);
        if (CurrentStep is null)
        {
            CompletedAt = now;
        }

        Revision++;
    }

    /// <summary>Sends the attempt back to its first Step. It keeps its start day and expiry time.</summary>
    public void Restart(DateTimeOffset now)
    {
        EnsureOpen(now);

        foreach (var step in _steps)
        {
            step.Reset();
        }

        Revision++;
    }

    /// <summary>Only an open attempt can be cancelled; cancelling removes it.</summary>
    public void EnsureCancellable(DateTimeOffset now) => EnsureOpen(now);

    /// <summary>A Parent overturns a complete attempt, so it no longer earns points. Allowed until <see cref="ExpiresAt"/>.</summary>
    public void Reject(Guid parentId, string? reason, DateTimeOffset now)
    {
        if (StateAt(now) != AttemptState.Complete)
        {
            throw new DomainRuleViolation(AttemptErrorCodes.NotComplete, "Only a complete attempt can be rejected.");
        }

        if (now >= ExpiresAt)
        {
            throw new DomainRuleViolation(
                AttemptErrorCodes.RejectionWindowClosed,
                "The attempt can no longer be rejected because its attempt window has passed.");
        }

        var trimmed = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (trimmed is { Length: > RejectionReasonMaxLength })
        {
            throw new DomainRuleViolation(
                AttemptErrorCodes.RejectionReasonTooLong,
                $"A rejection reason can be at most {RejectionReasonMaxLength} characters.");
        }

        RejectedAt = now;
        RejectedBy = parentId;
        RejectionReason = trimmed;
        Revision++;
    }

    private void EnsureOpen(DateTimeOffset now)
    {
        if (StateAt(now) != AttemptState.Open)
        {
            throw new DomainRuleViolation(AttemptErrorCodes.NotOpen, "The attempt is no longer open.");
        }
    }

#pragma warning disable CS8618 // Required by EF Core.
    private ChoreAttempt()
    {
    }
#pragma warning restore CS8618
}
