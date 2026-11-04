using Chores.Api.Tests.Infrastructure;
using Chores.Application.Common.Persistence;
using Chores.Domain.ChoreAttempts;
using Chores.Domain.ChoreLibrary;
using Chores.Domain.Families;
using Chores.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Chores.Api.Tests.Isolation;

/// <summary>Without a Family in the request (no <c>family_id</c> claim, no tenant scope), no Family data is visible or writable.</summary>
public class NoFamilyIdentityTests(PostgresApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Nothing_family_owned_can_be_read()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        var choreId = await factory.Services.SeedChoreAsync(seeded.Id);
        await factory.Services.SeedAttemptAsync(seeded.Id, seeded.Child.Id, choreId);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ChoresDbContext>();

        Assert.False(await db.Families.AnyAsync(Ct));
        Assert.False(await db.Members.AnyAsync(Ct));
        Assert.False(await db.Chores.AnyAsync(Ct));
        Assert.False(await db.Set<Step>().AnyAsync(Ct));
        Assert.False(await db.ChoreAttempts.AnyAsync(Ct));
        Assert.False(await db.Set<AttemptStep>().AnyAsync(Ct));
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<IChoreAttemptRepository>().ListAsync(null, DateOnly.MinValue, Ct));
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<IChoreRepository>().ListAsync(archived: false, Ct));
        Assert.Null(await scope.ServiceProvider.GetRequiredService<IFamilyRepository>().GetCurrentAsync(Ct));
    }

    [Fact]
    public async Task Nothing_family_owned_can_be_changed()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ChoresDbContext>();
        var member = await db.Members.IgnoreQueryFilters().SingleAsync(m => m.Id == seeded.Child.Id, Ct);

        member.ChangePreferredLanguage(PreferredLanguage.English);

        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(Ct));
    }
}
