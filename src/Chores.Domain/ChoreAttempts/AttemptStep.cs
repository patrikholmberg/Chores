using Chores.Domain.ChoreLibrary;
using Chores.Domain.Common;

namespace Chores.Domain.ChoreAttempts;

/// <summary>
/// A copy of one library Step taken when the attempt started, with when the Child completed it. Changed only through
/// <see cref="ChoreAttempt"/>.
/// </summary>
public sealed class AttemptStep : IFamilyOwned
{
    private AttemptStep(Guid id, Guid familyId, Guid attemptId, Step source)
    {
        Id = id;
        FamilyId = familyId;
        AttemptId = attemptId;
        Position = source.Position;
        Title = source.Title;
        Description = source.Description;
        ReminderDelay = source.ReminderDelay;
        RepeatInterval = source.RepeatInterval;
    }

    public Guid Id { get; private set; }

    public Guid FamilyId { get; private set; }

    public Guid AttemptId { get; private set; }

    /// <summary>0 for the first Step.</summary>
    public int Position { get; private set; }

    public string Title { get; private set; }

    public string? Description { get; private set; }

    /// <summary>When the Step Reminder is due, measured from the completion of the previous Step.</summary>
    public TimeSpan? ReminderDelay { get; private set; }

    /// <summary>How often the reminder repeats until the Step is done.</summary>
    public TimeSpan? RepeatInterval { get; private set; }

    /// <summary>When the Child completed the Step, or null while it is not done.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    public bool IsDone => CompletedAt is not null;

    internal static AttemptStep CopyOf(Guid familyId, Guid attemptId, Step source) =>
        new(Guid.CreateVersion7(), familyId, attemptId, source);

    internal void Complete(DateTimeOffset completedAt) => CompletedAt = completedAt;

    internal void Reset() => CompletedAt = null;

#pragma warning disable CS8618 // Required by EF Core.
    private AttemptStep()
    {
    }
#pragma warning restore CS8618
}
