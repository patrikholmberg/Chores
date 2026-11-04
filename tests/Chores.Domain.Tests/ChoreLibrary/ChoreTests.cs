using Chores.Domain.ChoreLibrary;
using Chores.Domain.Common;

namespace Chores.Domain.Tests.ChoreLibrary;

public class ChoreTests
{
    private static readonly Guid FamilyId = Guid.NewGuid();

    private static StepDefinition StepNamed(string title, int? delayMinutes = null, int? intervalMinutes = null) => new(
        title,
        null,
        delayMinutes is { } delay ? TimeSpan.FromMinutes(delay) : null,
        intervalMinutes is { } interval ? TimeSpan.FromMinutes(interval) : null);

    private static StepDefinition[] WashClothesSteps() =>
    [
        StepNamed("Wash clothes"),
        StepNamed("Hang clothes", delayMinutes: 90, intervalMinutes: 30),
        StepNamed("Collect clothes", delayMinutes: 2880),
    ];

    private static Chore NewChore(int pointMultiplier = 1, params StepDefinition[] steps) =>
        Chore.Create(Guid.NewGuid(), FamilyId, "Wash clothes", null, pointMultiplier, steps.Length > 0 ? steps : WashClothesSteps());

    private static void AssertViolation(string code, Action action) =>
        Assert.Equal(code, Assert.Throws<DomainRuleViolation>(action).Code);

    [Fact]
    public void Create_keeps_the_fields_and_the_step_order()
    {
        var chore = Chore.Create(Guid.NewGuid(), FamilyId, "  Wash clothes ", "  ", 2, WashClothesSteps());

        Assert.Equal("Wash clothes", chore.Title);
        Assert.Null(chore.Description);
        Assert.Equal(2, chore.PointMultiplier);
        Assert.Equal(["Wash clothes", "Hang clothes", "Collect clothes"], chore.Steps.Select(s => s.Title));
        Assert.Equal([0, 1, 2], chore.Steps.Select(s => s.Position));
        Assert.All(chore.Steps, s => Assert.Equal(FamilyId, s.FamilyId));
        Assert.All(chore.Steps, s => Assert.Equal(chore.Id, s.ChoreId));
    }

    [Fact]
    public void Create_keeps_the_reminder_settings()
    {
        var steps = NewChore().Steps;

        Assert.Null(steps[0].ReminderDelay);
        Assert.Equal(TimeSpan.FromMinutes(90), steps[1].ReminderDelay);
        Assert.Equal(TimeSpan.FromMinutes(30), steps[1].RepeatInterval);
        Assert.Equal(TimeSpan.FromHours(48), steps[2].ReminderDelay);
        Assert.Null(steps[2].RepeatInterval);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_title_is_refused(string title) =>
        AssertViolation(ChoreErrorCodes.TitleInvalid, () => Chore.Create(Guid.NewGuid(), FamilyId, title, null, 1, WashClothesSteps()));

    [Fact]
    public void A_too_long_title_is_refused() =>
        AssertViolation(ChoreErrorCodes.TitleInvalid, () =>
            Chore.Create(Guid.NewGuid(), FamilyId, new string('x', Chore.TitleMaxLength + 1), null, 1, WashClothesSteps()));

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Multipliers_from_1_to_100_are_accepted(int multiplier) =>
        Assert.Equal(multiplier, NewChore(multiplier).PointMultiplier);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public void A_multiplier_out_of_range_is_refused(int multiplier) =>
        AssertViolation(ChoreErrorCodes.MultiplierOutOfRange, () => NewChore(multiplier));

    [Fact]
    public void The_default_multiplier_is_1() => Assert.Equal(1, Chore.DefaultPointMultiplier);

    [Fact]
    public void A_chore_without_steps_is_refused() =>
        AssertViolation(ChoreErrorCodes.NoSteps, () => Chore.Create(Guid.NewGuid(), FamilyId, "Wash clothes", null, 1, []));

    [Fact]
    public void Twenty_steps_are_accepted_and_21_refused()
    {
        var steps = Enumerable.Range(1, Chore.MaxSteps + 1).Select(i => StepNamed($"Step {i}")).ToArray();

        Assert.Equal(20, NewChore(1, steps[..20]).Steps.Count);
        AssertViolation(ChoreErrorCodes.TooManySteps, () => NewChore(1, steps));
    }

    [Fact]
    public void A_blank_step_title_is_refused() =>
        AssertViolation(ChoreErrorCodes.StepTitleInvalid, () => NewChore(1, StepNamed(" ")));

    [Fact]
    public void A_delay_on_the_first_step_is_refused() =>
        AssertViolation(ChoreErrorCodes.FirstStepHasDelay, () => NewChore(1, StepNamed("Wash", delayMinutes: 10)));

    [Theory]
    [InlineData(0)]
    [InlineData(10081)]
    public void A_delay_out_of_range_is_refused(int minutes) =>
        AssertViolation(ChoreErrorCodes.DelayOutOfRange, () => NewChore(1, StepNamed("Wash"), StepNamed("Hang", delayMinutes: minutes)));

    [Fact]
    public void A_delay_that_is_not_whole_minutes_is_refused() =>
        AssertViolation(ChoreErrorCodes.DelayOutOfRange, () =>
            NewChore(1, StepNamed("Wash"), new StepDefinition("Hang", null, TimeSpan.FromSeconds(90), null)));

    [Theory]
    [InlineData(14)]
    [InlineData(10081)]
    public void An_interval_out_of_range_is_refused(int minutes) =>
        AssertViolation(ChoreErrorCodes.IntervalOutOfRange, () =>
            NewChore(1, StepNamed("Wash"), StepNamed("Hang", delayMinutes: 90, intervalMinutes: minutes)));

    [Fact]
    public void An_interval_without_a_delay_is_refused() =>
        AssertViolation(ChoreErrorCodes.IntervalWithoutDelay, () =>
            NewChore(1, StepNamed("Wash"), new StepDefinition("Hang", null, null, TimeSpan.FromMinutes(30))));

    [Fact]
    public void Revise_replaces_the_steps_and_increments_the_revision()
    {
        var chore = NewChore();
        var oldIds = chore.Steps.Select(s => s.Id).ToList();

        chore.Revise("Laundry", "All of it", 3, [StepNamed("Wash clothes"), StepNamed("Collect clothes", delayMinutes: 60)]);

        Assert.Equal("Laundry", chore.Title);
        Assert.Equal("All of it", chore.Description);
        Assert.Equal(3, chore.PointMultiplier);
        Assert.Equal(["Wash clothes", "Collect clothes"], chore.Steps.Select(s => s.Title));
        Assert.DoesNotContain(chore.Steps, s => oldIds.Contains(s.Id));
        Assert.Equal(1, chore.Revision);
    }

    [Fact]
    public void A_refused_revision_leaves_the_chore_unchanged()
    {
        var chore = NewChore();

        AssertViolation(ChoreErrorCodes.FirstStepHasDelay, () =>
            chore.Revise("Laundry", null, 1, [StepNamed("Collect clothes", delayMinutes: 2880), StepNamed("Wash clothes")]));

        Assert.Equal("Wash clothes", chore.Title);
        Assert.Equal(3, chore.Steps.Count);
        Assert.Equal(0, chore.Revision);
    }

    [Fact]
    public void Archive_and_restore_keep_every_field_and_step()
    {
        var chore = NewChore(2);
        var steps = chore.Steps.Select(s => (s.Id, s.Title, s.ReminderDelay, s.RepeatInterval)).ToList();

        chore.Archive(DateTimeOffset.UtcNow);
        Assert.True(chore.IsArchived);
        chore.Restore();

        Assert.False(chore.IsArchived);
        Assert.Equal("Wash clothes", chore.Title);
        Assert.Equal(2, chore.PointMultiplier);
        Assert.Equal(steps, chore.Steps.Select(s => (s.Id, s.Title, s.ReminderDelay, s.RepeatInterval)));
        Assert.Equal(2, chore.Revision);
    }

    [Fact]
    public void Archiving_twice_keeps_the_first_time()
    {
        var chore = NewChore();
        var first = new DateTimeOffset(2026, 11, 4, 10, 0, 0, TimeSpan.Zero);

        chore.Archive(first);
        chore.Archive(first.AddHours(1));

        Assert.Equal(first, chore.ArchivedAt);
        Assert.Equal(1, chore.Revision);
    }

    [Fact]
    public void Restoring_a_chore_in_the_library_changes_nothing()
    {
        var chore = NewChore();

        chore.Restore();

        Assert.Equal(0, chore.Revision);
    }

    [Fact]
    public void Revising_an_archived_chore_is_refused()
    {
        var chore = NewChore();
        chore.Archive(DateTimeOffset.UtcNow);

        AssertViolation(ChoreErrorCodes.Archived, () => chore.Revise("Laundry", null, 1, WashClothesSteps()));
        Assert.Equal("Wash clothes", chore.Title);
    }
}
