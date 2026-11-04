namespace Chores.Domain.ChoreLibrary;

/// <summary>Stable codes of <see cref="Common.DomainRuleViolation"/>s raised by the Chore aggregate.</summary>
public static class ChoreErrorCodes
{
    public const string TitleInvalid = "chore.title_invalid";
    public const string DescriptionTooLong = "chore.description_too_long";
    public const string MultiplierOutOfRange = "chore.multiplier_out_of_range";
    public const string NoSteps = "chore.no_steps";
    public const string TooManySteps = "chore.too_many_steps";
    public const string Archived = "chore.archived";
    public const string StepTitleInvalid = "step.title_invalid";
    public const string StepDescriptionTooLong = "step.description_too_long";
    public const string FirstStepHasDelay = "step.first_has_delay";
    public const string DelayOutOfRange = "step.delay_out_of_range";
    public const string IntervalOutOfRange = "step.interval_out_of_range";
    public const string IntervalWithoutDelay = "step.interval_without_delay";
}
