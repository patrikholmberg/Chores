using Chores.Api.Tests.Infrastructure;
using Chores.Application.Common.Exceptions;
using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Domain.ChoreAttempts;
using Chores.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Chores.Api.Tests.Persistence;

public class AttemptPersistenceTests(PostgresApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // Monday 2 November 2026 at 23:30 in Stockholm, which is still 22:30 on Monday in UTC.
    private static readonly DateTimeOffset LateMonday = new(2026, 11, 2, 22, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task An_attempt_reloads_with_its_steps_settings_completions_and_start_day()
    {
        var family = await factory.Services.SeedFamilyAsync();
        var choreId = await factory.Services.SeedChoreAsync(family.Id);
        var attemptId = await factory.Services.SeedAttemptAsync(family.Id, family.Child.Id, choreId, LateMonday);
        var completedAt = LateMonday.AddMinutes(10);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            using var _ = scope.ServiceProvider.GetRequiredService<TenantContext>().BeginScope(family.Id);
            var attempt = (await scope.ServiceProvider.GetRequiredService<IChoreAttemptRepository>().GetAsync(attemptId, Ct))!;
            attempt.CompleteStep(0, completedAt);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(Ct);
        }

        await using var check = factory.Services.CreateAsyncScope();
        using var __ = check.ServiceProvider.GetRequiredService<TenantContext>().BeginScope(family.Id);
        var reloaded = await check.ServiceProvider.GetRequiredService<IChoreAttemptRepository>().GetAsync(attemptId, Ct);

        Assert.NotNull(reloaded);
        Assert.Equal(new DateOnly(2026, 11, 2), reloaded.StartDay);
        Assert.Equal(LateMonday.AddDays(7), reloaded.ExpiresAt);
        Assert.Equal(["Wash clothes", "Hang clothes", "Collect clothes"], reloaded.Steps.Select(s => s.Title));
        Assert.Equal(completedAt, reloaded.Steps[0].CompletedAt);
        Assert.Null(reloaded.Steps[1].CompletedAt);
        Assert.Equal(TimeSpan.FromMinutes(90), reloaded.Steps[1].ReminderDelay);
        Assert.Equal(TimeSpan.FromMinutes(30), reloaded.Steps[1].RepeatInterval);
        Assert.Equal(TimeSpan.FromHours(48), reloaded.Steps[2].ReminderDelay);
        Assert.All(reloaded.Steps, s => Assert.Equal(family.Id, s.FamilyId));
        Assert.Equal(1, reloaded.CurrentStep!.Position);
    }

    [Fact]
    public async Task Removing_an_attempt_deletes_its_step_rows()
    {
        var family = await factory.Services.SeedFamilyAsync();
        var choreId = await factory.Services.SeedChoreAsync(family.Id);
        var attemptId = await factory.Services.SeedAttemptAsync(family.Id, family.Child.Id, choreId);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            using var _ = scope.ServiceProvider.GetRequiredService<TenantContext>().BeginScope(family.Id);
            var attempts = scope.ServiceProvider.GetRequiredService<IChoreAttemptRepository>();
            attempts.Remove((await attempts.GetAsync(attemptId, Ct))!);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(Ct);
        }

        await using var check = factory.Services.CreateAsyncScope();
        using var __ = check.ServiceProvider.GetRequiredService<TenantContext>().BeginScope(family.Id);
        var db = check.ServiceProvider.GetRequiredService<ChoresDbContext>();
        Assert.False(await db.ChoreAttempts.AnyAsync(a => a.Id == attemptId, Ct));
        Assert.False(await db.Set<AttemptStep>().AnyAsync(s => s.AttemptId == attemptId, Ct));
    }

    [Fact]
    public async Task Archiving_the_source_chore_leaves_the_attempt_loadable()
    {
        var family = await factory.Services.SeedFamilyAsync();
        var choreId = await factory.Services.SeedChoreAsync(family.Id);
        var attemptId = await factory.Services.SeedAttemptAsync(family.Id, family.Child.Id, choreId);

        await using var scope = factory.Services.CreateAsyncScope();
        using var _ = scope.ServiceProvider.GetRequiredService<TenantContext>().BeginScope(family.Id);
        (await scope.ServiceProvider.GetRequiredService<IChoreRepository>().GetAsync(choreId, Ct))!.Archive(DateTimeOffset.UtcNow);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(Ct);

        var attempt = await scope.ServiceProvider.GetRequiredService<IChoreAttemptRepository>().GetAsync(attemptId, Ct);
        Assert.NotNull(attempt);
        Assert.Equal(3, attempt.Steps.Count);
    }

    [Fact]
    public async Task Lists_are_by_child_and_start_day_newest_first()
    {
        var family = await factory.Services.SeedFamilyAsync();
        var choreId = await factory.Services.SeedChoreAsync(family.Id);
        var older = await factory.Services.SeedAttemptAsync(family.Id, family.Child.Id, choreId, LateMonday.AddDays(-3));
        var monday = await factory.Services.SeedAttemptAsync(family.Id, family.Child.Id, choreId, LateMonday);
        var mondayLater = await factory.Services.SeedAttemptAsync(family.Id, family.Child.Id, choreId, LateMonday.AddMinutes(5));

        await using var scope = factory.Services.CreateAsyncScope();
        using var _ = scope.ServiceProvider.GetRequiredService<TenantContext>().BeginScope(family.Id);
        var attempts = scope.ServiceProvider.GetRequiredService<IChoreAttemptRepository>();

        Assert.Equal([mondayLater, monday], (await attempts.ListAsync(family.Child.Id, new DateOnly(2026, 11, 2), Ct)).Select(a => a.Id));
        Assert.Equal([mondayLater, monday, older], (await attempts.ListAsync(null, new DateOnly(2026, 10, 1), Ct)).Select(a => a.Id));
        Assert.Empty(await attempts.ListAsync(Guid.NewGuid(), new DateOnly(2026, 10, 1), Ct));
        Assert.Equal(
            [monday, mondayLater],
            (await attempts.ListStartedOnAsync(new DateOnly(2026, 11, 2), Ct)).Select(a => a.Id).Order());
    }

    [Fact]
    public async Task The_second_of_two_concurrent_completions_conflicts()
    {
        var family = await factory.Services.SeedFamilyAsync();
        var choreId = await factory.Services.SeedChoreAsync(family.Id);
        var attemptId = await factory.Services.SeedAttemptAsync(family.Id, family.Child.Id, choreId);
        await using var first = factory.Services.CreateAsyncScope();
        await using var second = factory.Services.CreateAsyncScope();
        using var _ = first.ServiceProvider.GetRequiredService<TenantContext>().BeginScope(family.Id);
        using var __ = second.ServiceProvider.GetRequiredService<TenantContext>().BeginScope(family.Id);

        var firstAttempt = (await first.ServiceProvider.GetRequiredService<IChoreAttemptRepository>().GetAsync(attemptId, Ct))!;
        var secondAttempt = (await second.ServiceProvider.GetRequiredService<IChoreAttemptRepository>().GetAsync(attemptId, Ct))!;
        firstAttempt.CompleteStep(0, DateTimeOffset.UtcNow);
        secondAttempt.CompleteStep(0, DateTimeOffset.UtcNow);

        await first.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(Ct);
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            second.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(Ct));
    }
}
