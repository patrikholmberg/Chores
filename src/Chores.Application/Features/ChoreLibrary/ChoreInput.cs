using Chores.Application.Common.Validation;
using Chores.Domain.ChoreLibrary;
using FluentValidation;

namespace Chores.Application.Features.ChoreLibrary;

/// <summary>A Step as a Parent enters it, with reminder settings in whole minutes.</summary>
public sealed record StepInput(string Title, string? Description, int? ReminderDelayMinutes, int? RepeatIntervalMinutes)
{
    public StepDefinition ToDefinition() => new(
        Title,
        Description,
        ReminderDelayMinutes is { } delay ? TimeSpan.FromMinutes(delay) : null,
        RepeatIntervalMinutes is { } interval ? TimeSpan.FromMinutes(interval) : null);
}

/// <summary>The fields a Parent enters when creating or editing a Chore.</summary>
public interface IChoreInput
{
    string Title { get; }

    string? Description { get; }

    int? PointMultiplier { get; }

    IReadOnlyList<StepInput> Steps { get; }
}

/// <summary>The Chore rules from <see cref="Chore"/> and <see cref="Step"/>, reported per field.</summary>
public abstract class ChoreInputValidator<T> : AbstractValidator<T>
    where T : IChoreInput
{
    protected ChoreInputValidator()
    {
        RuleFor(c => c.Title).Title(Chore.TitleMaxLength);
        RuleFor(c => c.Description).MaximumLength(Chore.DescriptionMaxLength).WithErrorCode(ValidationCodes.TooLong);
        RuleFor(c => c.PointMultiplier)
            .InclusiveBetween(Chore.MinPointMultiplier, Chore.MaxPointMultiplier)
            .WithErrorCode(ValidationCodes.MultiplierOutOfRange)
            .WithMessage($"The Point Multiplier must be a whole number from {Chore.MinPointMultiplier} to {Chore.MaxPointMultiplier}.");

        RuleFor(c => c.Steps)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithErrorCode(ValidationCodes.NoSteps).WithMessage("A Chore needs at least one Step.")
            .Must(steps => steps.Count <= Chore.MaxSteps).WithErrorCode(ValidationCodes.TooManySteps)
            .WithMessage($"A Chore can have at most {Chore.MaxSteps} Steps.");
        RuleForEach(c => c.Steps)
            .NotNull().WithErrorCode(ValidationCodes.Required)
            .SetValidator(new StepInputValidator())
            .When(c => c.Steps is not null);
        RuleFor(c => c.Steps[0].ReminderDelayMinutes)
            .Null().WithErrorCode(ValidationCodes.DelayOnFirstStep).WithMessage("The first Step cannot have a reminder delay.")
            .OverridePropertyName("Steps[0].ReminderDelayMinutes")
            .When(c => c.Steps is { Count: > 0 } && c.Steps[0] is not null);
    }
}

public sealed class StepInputValidator : AbstractValidator<StepInput>
{
    public StepInputValidator()
    {
        RuleFor(s => s.Title).Title(Step.TitleMaxLength);
        RuleFor(s => s.Description).MaximumLength(Step.DescriptionMaxLength).WithErrorCode(ValidationCodes.TooLong);
        RuleFor(s => s.ReminderDelayMinutes)
            .InclusiveBetween((int)Step.MinReminderDelay.TotalMinutes, (int)Step.MaxReminderDelay.TotalMinutes)
            .WithErrorCode(ValidationCodes.DelayOutOfRange)
            .WithMessage($"A reminder delay must be {Step.MinReminderDelay.TotalMinutes} to {Step.MaxReminderDelay.TotalMinutes} minutes.");
        RuleFor(s => s.RepeatIntervalMinutes)
            .Null().WithErrorCode(ValidationCodes.IntervalWithoutDelay).WithMessage("A repeat interval needs a reminder delay.")
            .When(s => s.ReminderDelayMinutes is null, ApplyConditionTo.CurrentValidator)
            .InclusiveBetween((int)Step.MinRepeatInterval.TotalMinutes, (int)Step.MaxRepeatInterval.TotalMinutes)
            .WithErrorCode(ValidationCodes.IntervalOutOfRange)
            .WithMessage($"A repeat interval must be {Step.MinRepeatInterval.TotalMinutes} to {Step.MaxRepeatInterval.TotalMinutes} minutes.");
    }
}
