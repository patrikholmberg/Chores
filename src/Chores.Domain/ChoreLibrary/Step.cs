using Chores.Domain.Common;

namespace Chores.Domain.ChoreLibrary;

/// <summary>What a Parent gives for one Step when creating or revising a Chore, in the order the Steps are done.</summary>
public sealed record StepDefinition(string Title, string? Description, TimeSpan? ReminderDelay, TimeSpan? RepeatInterval);

/// <summary>One ordered part of a Chore. Changed only through <see cref="Chore"/>.</summary>
public sealed class Step : IFamilyOwned
{
    public const int TitleMaxLength = 100;
    public const int DescriptionMaxLength = 1000;

    public static readonly TimeSpan MinReminderDelay = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan MaxReminderDelay = TimeSpan.FromDays(7);
    public static readonly TimeSpan MinRepeatInterval = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan MaxRepeatInterval = TimeSpan.FromDays(7);

    private Step(Guid id, Guid familyId, Guid choreId, int position, string title, string? description, TimeSpan? reminderDelay, TimeSpan? repeatInterval)
    {
        Id = id;
        FamilyId = familyId;
        ChoreId = choreId;
        Position = position;
        Title = title;
        Description = description;
        ReminderDelay = reminderDelay;
        RepeatInterval = repeatInterval;
    }

    public Guid Id { get; private set; }

    public Guid FamilyId { get; private set; }

    public Guid ChoreId { get; private set; }

    /// <summary>0 for the first Step.</summary>
    public int Position { get; private set; }

    public string Title { get; private set; }

    public string? Description { get; private set; }

    /// <summary>When the Step Reminder is due, measured from the completion of the previous Step. Never set on the first Step.</summary>
    public TimeSpan? ReminderDelay { get; private set; }

    /// <summary>How often the reminder repeats until the Step is done. Only set together with <see cref="ReminderDelay"/>.</summary>
    public TimeSpan? RepeatInterval { get; private set; }

    internal static Step Create(Guid familyId, Guid choreId, int position, StepDefinition definition)
    {
        var title = Text.Required(definition.Title, TitleMaxLength, ChoreErrorCodes.StepTitleInvalid, "A Step");
        var description = Text.Optional(definition.Description, DescriptionMaxLength, ChoreErrorCodes.StepDescriptionTooLong, "A Step");

        if (position == 0 && definition.ReminderDelay is not null)
        {
            throw new DomainRuleViolation(ChoreErrorCodes.FirstStepHasDelay, "The first Step cannot have a reminder delay.");
        }

        if (definition.ReminderDelay is { } delay && !IsWholeMinutesBetween(delay, MinReminderDelay, MaxReminderDelay))
        {
            throw new DomainRuleViolation(
                ChoreErrorCodes.DelayOutOfRange,
                $"A reminder delay must be whole minutes from {MinReminderDelay.TotalMinutes} to {MaxReminderDelay.TotalMinutes}.");
        }

        if (definition.RepeatInterval is { } interval)
        {
            if (definition.ReminderDelay is null)
            {
                throw new DomainRuleViolation(ChoreErrorCodes.IntervalWithoutDelay, "A repeat interval needs a reminder delay.");
            }

            if (!IsWholeMinutesBetween(interval, MinRepeatInterval, MaxRepeatInterval))
            {
                throw new DomainRuleViolation(
                    ChoreErrorCodes.IntervalOutOfRange,
                    $"A repeat interval must be whole minutes from {MinRepeatInterval.TotalMinutes} to {MaxRepeatInterval.TotalMinutes}.");
            }
        }

        return new Step(Guid.CreateVersion7(), familyId, choreId, position, title, description, definition.ReminderDelay, definition.RepeatInterval);
    }

    private static bool IsWholeMinutesBetween(TimeSpan value, TimeSpan min, TimeSpan max) =>
        value.Ticks % TimeSpan.TicksPerMinute == 0 && value >= min && value <= max;

#pragma warning disable CS8618 // Required by EF Core.
    private Step()
    {
    }
#pragma warning restore CS8618
}
