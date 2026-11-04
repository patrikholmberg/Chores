using Chores.Application.Common.Behaviors;
using Chores.Application.Common.Exceptions;
using Chores.Application.Common.Validation;
using Chores.Application.Features.FamilySettings;
using Chores.Application.Tests.Common;
using Chores.Application.Tests.Features.ChoreLibrary;
using Chores.Domain.Families;
using Mediator;

namespace Chores.Application.Tests.Features.FamilySettings;

public class FamilySettingsTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static IEnumerable<string> CodesFor(UpdateFamilySettingsCommand command, string property) =>
        new UpdateFamilySettingsValidator().Validate(command).Errors.Where(e => e.PropertyName == property).Select(e => e.ErrorCode);

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1000, 30)]
    public void Values_within_range_pass(int basePoints, int days) =>
        Assert.True(new UpdateFamilySettingsValidator().Validate(new UpdateFamilySettingsCommand(basePoints, days)).IsValid);

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public void Base_points_out_of_range_are_reported_on_that_field(int basePoints) =>
        Assert.Contains(ValidationCodes.BasePointsOutOfRange, CodesFor(new UpdateFamilySettingsCommand(basePoints, 7), "BasePoints"));

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public void An_attempt_window_out_of_range_is_reported_on_that_field(int days) =>
        Assert.Contains(
            ValidationCodes.AttemptWindowOutOfRange,
            CodesFor(new UpdateFamilySettingsCommand(10, days), "AttemptWindowDays"));

    [Fact]
    public async Task The_handler_saves_and_returns_the_new_settings()
    {
        var (family, _) = ChoreTestData.FamilyWithParent(PreferredLanguage.English);
        var unitOfWork = new FakeUnitOfWork();

        var result = await new UpdateFamilySettingsHandler(new FakeFamilyRepository(family), unitOfWork)
            .Handle(new UpdateFamilySettingsCommand(5, 3), Ct);

        Assert.Equal(new FamilySettingsDto(5, 3), result);
        Assert.Equal(TimeSpan.FromDays(3), family.AttemptWindow);
        Assert.Equal(1, unitOfWork.Saves);
    }

    [Fact]
    public async Task A_new_family_reads_the_defaults()
    {
        var (family, _) = ChoreTestData.FamilyWithParent(PreferredLanguage.English);

        var result = await new GetFamilySettingsHandler(new FakeFamilyRepository(family)).Handle(new GetFamilySettingsQuery(), Ct);

        Assert.Equal(new FamilySettingsDto(10, 7), result);
    }

    [Fact]
    public async Task A_child_is_refused_both_messages()
    {
        await AssertRefusedAsync(new GetFamilySettingsQuery());
        await AssertRefusedAsync(new UpdateFamilySettingsCommand(10, 7));
    }

    private static async Task AssertRefusedAsync<TMessage>(TMessage message)
        where TMessage : IMessage
    {
        var behavior = new AuthorizationBehavior<TMessage, FamilySettingsDto>(new FakeCurrentMember(Role.Child));
        await Assert.ThrowsAsync<ForbiddenException>(async () => await behavior.Handle(
            message,
            (_, _) => throw new InvalidOperationException("The handler must not run."),
            Ct));
    }
}
