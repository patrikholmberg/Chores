using Chores.Application.Common.Behaviors;
using Chores.Application.Common.Exceptions;
using Chores.Application.Common.Tenancy;
using Chores.Application.Common.Validation;
using Chores.Application.Features.ChoreLibrary;
using Chores.Application.Tests.Common;
using Chores.Domain.Families;
using Mediator;

namespace Chores.Application.Tests.Features.ChoreLibrary;

public class ChoreCommandTests
{
    private static readonly StepInput Wash = new("Wash clothes", null, null, null);
    private static readonly StepInput Hang = new("Hang clothes", null, 90, 30);

    private static readonly CreateChoreCommand Valid = new("Wash clothes", null, 2, [Wash, Hang]);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static IEnumerable<string> CodesFor(CreateChoreCommand command, string property) =>
        new CreateChoreValidator().Validate(command).Errors.Where(e => e.PropertyName == property).Select(e => e.ErrorCode);

    [Fact]
    public void A_valid_chore_passes() => Assert.True(new CreateChoreValidator().Validate(Valid).IsValid);

    [Fact]
    public void A_blank_title_is_reported_on_the_title() =>
        Assert.Contains(ValidationCodes.Required, CodesFor(Valid with { Title = " " }, "Title"));

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void A_multiplier_out_of_range_is_reported(int multiplier) =>
        Assert.Contains(ValidationCodes.MultiplierOutOfRange, CodesFor(Valid with { PointMultiplier = multiplier }, "PointMultiplier"));

    [Fact]
    public void No_steps_are_reported_on_the_steps() =>
        Assert.Contains(ValidationCodes.NoSteps, CodesFor(Valid with { Steps = [] }, "Steps"));

    [Fact]
    public void Missing_steps_are_reported_on_the_steps() =>
        Assert.Contains(ValidationCodes.NoSteps, CodesFor(Valid with { Steps = null! }, "Steps"));

    [Fact]
    public void Too_many_steps_are_reported_on_the_steps() =>
        Assert.Contains(
            ValidationCodes.TooManySteps,
            CodesFor(Valid with { Steps = Enumerable.Repeat(Wash, 21).ToList() }, "Steps"));

    [Fact]
    public void A_delay_on_the_first_step_is_reported_on_that_field() =>
        Assert.Contains(
            ValidationCodes.DelayOnFirstStep,
            CodesFor(Valid with { Steps = [Hang, Wash] }, "Steps[0].ReminderDelayMinutes"));

    [Fact]
    public void A_delay_out_of_range_is_reported_on_that_step() =>
        Assert.Contains(
            ValidationCodes.DelayOutOfRange,
            CodesFor(Valid with { Steps = [Wash, Hang with { ReminderDelayMinutes = 10081 }] }, "Steps[1].ReminderDelayMinutes"));

    [Fact]
    public void An_interval_without_a_delay_is_reported_on_that_step() =>
        Assert.Contains(
            ValidationCodes.IntervalWithoutDelay,
            CodesFor(Valid with { Steps = [Wash, Hang with { ReminderDelayMinutes = null }] }, "Steps[1].RepeatIntervalMinutes"));

    [Fact]
    public void An_interval_too_short_is_reported_on_that_step() =>
        Assert.Contains(
            ValidationCodes.IntervalOutOfRange,
            CodesFor(Valid with { Steps = [Wash, Hang with { RepeatIntervalMinutes = 10 }] }, "Steps[1].RepeatIntervalMinutes"));

    [Fact]
    public void A_blank_step_title_is_reported_on_that_step() =>
        Assert.Contains(ValidationCodes.Required, CodesFor(Valid with { Steps = [Wash with { Title = "" }] }, "Steps[0].Title"));

    [Fact]
    public void An_edit_needs_a_multiplier()
    {
        var command = new UpdateChoreCommand(Guid.NewGuid(), 0, "Wash clothes", null, null, [Wash]);

        Assert.Contains(new UpdateChoreValidator().Validate(command).Errors, e => e.PropertyName == "PointMultiplier");
    }

    [Fact]
    public async Task A_missing_multiplier_becomes_1()
    {
        var repository = new FakeChoreRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tenant = new TenantContext(new FakeCurrentMember(Role.Parent, ChoreTestData.FamilyId));

        var dto = await new CreateChoreHandler(repository, tenant, unitOfWork).Handle(Valid with { PointMultiplier = null }, Ct);

        Assert.Equal(1, dto.PointMultiplier);
        Assert.Equal(1, unitOfWork.Saves);
        Assert.NotNull(await repository.GetAsync(dto.Id, Ct));
    }

    [Fact]
    public async Task A_stale_revision_conflicts_and_changes_nothing()
    {
        var chore = ChoreTestData.NewChore();
        chore.Revise("Laundry", null, 1, [Wash.ToDefinition()]);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpdateChoreHandler(new FakeChoreRepository(chore), unitOfWork);

        await Assert.ThrowsAsync<ConcurrencyConflictException>(async () =>
            await handler.Handle(new UpdateChoreCommand(chore.Id, 0, "Mine", null, 1, [Wash]), Ct));

        Assert.Equal("Laundry", chore.Title);
        Assert.Equal(0, unitOfWork.Saves);
    }

    public static TheoryData<IMessage> ParentOnlyMessages() =>
    [
        Valid,
        new UpdateChoreCommand(Guid.NewGuid(), 0, "Wash clothes", null, 1, [Wash]),
        new ArchiveChoreCommand(Guid.NewGuid()),
        new RestoreChoreCommand(Guid.NewGuid()),
        new ListArchivedChoresQuery(),
    ];

    [Theory]
    [MemberData(nameof(ParentOnlyMessages))]
    public async Task A_child_is_refused_before_the_handler_runs(IMessage message)
    {
        var invoked = false;
        var behavior = new AuthorizationBehavior<IMessage, object>(new FakeCurrentMember(Role.Child, ChoreTestData.FamilyId));

        await Assert.ThrowsAsync<ForbiddenException>(async () => await behavior.Handle(
            message,
            (_, _) => { invoked = true; return ValueTask.FromResult<object>("ok"); },
            Ct));

        Assert.False(invoked);
    }

    [Fact]
    public void The_library_and_a_single_chore_are_open_to_children()
    {
        Assert.IsNotAssignableFrom<IParentOnly>(new ListChoresQuery());
        Assert.IsNotAssignableFrom<IParentOnly>(new GetChoreQuery(Guid.NewGuid()));
    }
}
