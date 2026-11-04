using Chores.Application.Common.Exceptions;
using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Domain.ChoreAttempts;
using Chores.Domain.Families;

namespace Chores.Application.Features.ChoreAttempts;

/// <summary>Who rejected an attempt, when and why.</summary>
public sealed record RejectionDto(DateTimeOffset At, Guid ByMemberId, string ByName, string? Reason);

/// <summary>An attempt as it appears in a list: its state and where the Child is in it, without the Steps.</summary>
public sealed record AttemptSummaryDto(
    Guid Id,
    Guid ChildId,
    string ChildName,
    Guid ChoreId,
    string Title,
    int PointMultiplier,
    DateOnly StartDay,
    DateTimeOffset StartedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? CompletedAt,
    AttemptState State,
    int StepCount,
    int? CurrentStepPosition,
    string? CurrentStepTitle,
    RejectionDto? Rejection)
{
    public static AttemptSummaryDto From(ChoreAttempt attempt, MemberNames names, DateTimeOffset now) => new(
        attempt.Id,
        attempt.ChildId,
        names[attempt.ChildId],
        attempt.ChoreId,
        attempt.Title,
        attempt.PointMultiplier,
        attempt.StartDay,
        attempt.StartedAt,
        attempt.ExpiresAt,
        attempt.CompletedAt,
        attempt.StateAt(now),
        attempt.Steps.Count,
        attempt.CurrentStep?.Position,
        attempt.CurrentStep?.Title,
        RejectionOf(attempt, names));

    internal static RejectionDto? RejectionOf(ChoreAttempt attempt, MemberNames names) =>
        attempt is { RejectedAt: { } at, RejectedBy: { } by }
            ? new RejectionDto(at, by, names[by], attempt.RejectionReason)
            : null;
}

/// <summary>An attempt with its snapshotted Steps and when each was completed.</summary>
public sealed record AttemptDto(
    Guid Id,
    Guid ChildId,
    string ChildName,
    Guid ChoreId,
    string Title,
    string? Description,
    int PointMultiplier,
    DateOnly StartDay,
    DateTimeOffset StartedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? CompletedAt,
    AttemptState State,
    int? CurrentStepPosition,
    RejectionDto? Rejection,
    int Revision,
    IReadOnlyList<AttemptStepDto> Steps)
{
    public static AttemptDto From(ChoreAttempt attempt, MemberNames names, DateTimeOffset now) => new(
        attempt.Id,
        attempt.ChildId,
        names[attempt.ChildId],
        attempt.ChoreId,
        attempt.Title,
        attempt.Description,
        attempt.PointMultiplier,
        attempt.StartDay,
        attempt.StartedAt,
        attempt.ExpiresAt,
        attempt.CompletedAt,
        attempt.StateAt(now),
        attempt.CurrentStep?.Position,
        AttemptSummaryDto.RejectionOf(attempt, names),
        attempt.Revision,
        attempt.Steps.Select(AttemptStepDto.From).ToList());
}

/// <summary>A snapshotted Step with its reminder settings in whole minutes.</summary>
public sealed record AttemptStepDto(
    int Position,
    string Title,
    string? Description,
    int? ReminderDelayMinutes,
    int? RepeatIntervalMinutes,
    DateTimeOffset? CompletedAt)
{
    public static AttemptStepDto From(AttemptStep step) => new(
        step.Position,
        step.Title,
        step.Description,
        Minutes(step.ReminderDelay),
        Minutes(step.RepeatInterval),
        step.CompletedAt);

    private static int? Minutes(TimeSpan? value) => value is { } v ? (int)v.TotalMinutes : null;
}

/// <summary>Display names of a Family's current Members. A removed Member has no name.</summary>
public sealed class MemberNames(Family family)
{
    private readonly Dictionary<Guid, string> _names = family.Members.ToDictionary(m => m.Id, m => m.DisplayName);

    public string this[Guid memberId] => _names.GetValueOrDefault(memberId, string.Empty);
}

internal static class AttemptLookup
{
    /// <summary>
    /// An attempt the current Member may see: a Parent sees every attempt of the Family, a Child only their own. Any other
    /// attempt is reported as not found.
    /// </summary>
    public static async Task<ChoreAttempt> LoadVisibleAsync(
        this IChoreAttemptRepository attempts,
        Guid attemptId,
        ICurrentMember currentMember,
        CancellationToken cancellationToken)
    {
        var attempt = await attempts.GetAsync(attemptId, cancellationToken);
        return attempt is not null && (currentMember.Role == Role.Parent || attempt.ChildId == currentMember.MemberId)
            ? attempt
            : throw new NotFoundException("Chore Attempt", attemptId);
    }
}
