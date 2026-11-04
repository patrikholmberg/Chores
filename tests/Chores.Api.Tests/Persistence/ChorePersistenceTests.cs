using Chores.Api.Tests.Infrastructure;
using Chores.Application.Common.Exceptions;
using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Domain.ChoreLibrary;
using Chores.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Chores.Api.Tests.Persistence;

public class ChorePersistenceTests(PostgresApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_saved_chore_reloads_with_its_steps_in_order()
    {
        var family = await factory.Services.SeedFamilyAsync();
        var choreId = await factory.Services.SeedChoreAsync(family.Id);

        await using var scope = factory.Services.CreateAsyncScope();
        using var _ = scope.ServiceProvider.GetRequiredService<TenantContext>().BeginScope(family.Id);
        var chore = await scope.ServiceProvider.GetRequiredService<IChoreRepository>().GetAsync(choreId, Ct);

        Assert.NotNull(chore);
        Assert.Equal(["Wash clothes", "Hang clothes", "Collect clothes"], chore.Steps.Select(s => s.Title));
        Assert.Null(chore.Steps[0].ReminderDelay);
        Assert.Equal(TimeSpan.FromMinutes(90), chore.Steps[1].ReminderDelay);
        Assert.Equal(TimeSpan.FromMinutes(30), chore.Steps[1].RepeatInterval);
        Assert.Equal(TimeSpan.FromHours(48), chore.Steps[2].ReminderDelay);
    }

    [Fact]
    public async Task Revising_deletes_the_old_step_rows_and_stamps_the_family()
    {
        var family = await factory.Services.SeedFamilyAsync();
        var choreId = await factory.Services.SeedChoreAsync(family.Id);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            using var _ = scope.ServiceProvider.GetRequiredService<TenantContext>().BeginScope(family.Id);
            var chore = (await scope.ServiceProvider.GetRequiredService<IChoreRepository>().GetAsync(choreId, Ct))!;
            chore.Revise("Laundry", null, 2, [new StepDefinition("Wash", null, null, null), new StepDefinition("Collect", null, TimeSpan.FromHours(1), null)]);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(Ct);
        }

        await using var check = factory.Services.CreateAsyncScope();
        using var __ = check.ServiceProvider.GetRequiredService<TenantContext>().BeginScope(family.Id);
        var db = check.ServiceProvider.GetRequiredService<ChoresDbContext>();
        var steps = await db.Set<Step>().Where(s => s.ChoreId == choreId).OrderBy(s => s.Position).ToListAsync(Ct);

        Assert.Equal(["Wash", "Collect"], steps.Select(s => s.Title));
        Assert.All(steps, s => Assert.Equal(family.Id, s.FamilyId));
        Assert.Equal(family.Id, (await db.Chores.SingleAsync(c => c.Id == choreId, Ct)).FamilyId);
    }

    [Fact]
    public async Task The_second_of_two_concurrent_saves_conflicts()
    {
        var family = await factory.Services.SeedFamilyAsync();
        var choreId = await factory.Services.SeedChoreAsync(family.Id);
        await using var first = factory.Services.CreateAsyncScope();
        await using var second = factory.Services.CreateAsyncScope();
        using var _ = first.ServiceProvider.GetRequiredService<TenantContext>().BeginScope(family.Id);
        using var __ = second.ServiceProvider.GetRequiredService<TenantContext>().BeginScope(family.Id);

        var firstChore = (await first.ServiceProvider.GetRequiredService<IChoreRepository>().GetAsync(choreId, Ct))!;
        var secondChore = (await second.ServiceProvider.GetRequiredService<IChoreRepository>().GetAsync(choreId, Ct))!;
        firstChore.Revise("First", null, 1, TestData.WashClothesSteps());
        secondChore.Revise("Second", null, 1, TestData.WashClothesSteps());

        await first.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(Ct);
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            second.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(Ct));
    }

    [Fact]
    public async Task Archived_and_library_chores_are_listed_separately()
    {
        var family = await factory.Services.SeedFamilyAsync();
        var kept = await factory.Services.SeedChoreAsync(family.Id, "Kept");
        var archived = await factory.Services.SeedChoreAsync(family.Id, "Archived");

        await using var scope = factory.Services.CreateAsyncScope();
        using var _ = scope.ServiceProvider.GetRequiredService<TenantContext>().BeginScope(family.Id);
        var chores = scope.ServiceProvider.GetRequiredService<IChoreRepository>();
        (await chores.GetAsync(archived, Ct))!.Archive(DateTimeOffset.UtcNow);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(Ct);

        Assert.Equal([kept], (await chores.ListAsync(archived: false, Ct)).Select(c => c.Id));
        Assert.Equal([archived], (await chores.ListAsync(archived: true, Ct)).Select(c => c.Id));
    }
}
