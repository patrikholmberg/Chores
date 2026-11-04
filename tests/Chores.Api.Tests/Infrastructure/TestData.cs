using Chores.Application.Common.Identity;
using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Domain.ChoreLibrary;
using Chores.Domain.Families;
using Microsoft.Extensions.DependencyInjection;

namespace Chores.Api.Tests.Infrastructure;

public sealed record SeededMember(Guid Id, string Username, string Password, Role Role);

public sealed record SeededFamily(Guid Id, SeededMember Parent, SeededMember Child);

/// <summary>Creates Families directly through the Application ports, bypassing the Api.</summary>
public static class TestData
{
    public const string ParentPassword = "parent-password";
    public const string ChildPassword = "kid1";

    /// <summary>A username that is unique across the shared test database.</summary>
    public static string UniqueUsername(string prefix = "m") => $"{prefix}-{Guid.NewGuid():N}"[..24];

    /// <summary>Loads the Family in its own scope, applies <paramref name="change"/> and saves.</summary>
    public static async Task ChangeFamilyAsync(this IServiceProvider services, Guid familyId, Func<Family, IServiceProvider, Task> change)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        using var _ = provider.GetRequiredService<TenantContext>().BeginScope(familyId);

        var family = await provider.GetRequiredService<IFamilyRepository>().GetCurrentAsync(CancellationToken.None);
        await change(family!, provider);
        await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);
    }

    /// <summary>Adds a second Parent to a seeded Family.</summary>
    public static async Task<SeededMember> AddParentAsync(this IServiceProvider services, Guid familyId)
    {
        var parent = new SeededMember(Guid.CreateVersion7(), UniqueUsername("p"), ParentPassword, Role.Parent);
        await services.ChangeFamilyAsync(familyId, async (family, provider) =>
        {
            family.AddMember(parent.Id, Username.Create(parent.Username), null, Role.Parent, "second@example.com", PreferredLanguage.English);
            await provider.GetRequiredService<IMemberCredentials>()
                .CreateAsync(parent.Id, Username.Create(parent.Username), "second@example.com", parent.Password, CancellationToken.None);
        });
        return parent;
    }

    /// <summary>Seeds a Family with one Parent and one Child.</summary>
    public static async Task<SeededFamily> SeedFamilyAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var familyId = Guid.CreateVersion7();
        using var _ = provider.GetRequiredService<TenantContext>().BeginScope(familyId);

        var parent = new SeededMember(Guid.CreateVersion7(), UniqueUsername("p"), ParentPassword, Role.Parent);
        var child = new SeededMember(Guid.CreateVersion7(), UniqueUsername("c"), ChildPassword, Role.Child);

        var family = Family.Register(
            familyId, "Test family", FamilyTimeZone.Create("Europe/Stockholm"),
            parent.Id, Username.Create(parent.Username), "Parent", "parent@example.com", PreferredLanguage.English);
        family.AddMember(child.Id, Username.Create(child.Username), "Child", Role.Child, null, PreferredLanguage.Swedish);
        provider.GetRequiredService<IFamilyRepository>().Add(family);

        var credentials = provider.GetRequiredService<IMemberCredentials>();
        await credentials.CreateAsync(parent.Id, Username.Create(parent.Username), "parent@example.com", parent.Password, CancellationToken.None);
        await credentials.CreateAsync(child.Id, Username.Create(child.Username), null, child.Password, CancellationToken.None);

        await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);
        return new SeededFamily(familyId, parent, child);
    }

    /// <summary>The Steps of the spec's "Wash clothes" example: 90 minutes to hang (repeating every 30), 48 hours to collect.</summary>
    public static StepDefinition[] WashClothesSteps() =>
    [
        new("Wash clothes", null, null, null),
        new("Hang clothes", null, TimeSpan.FromMinutes(90), TimeSpan.FromMinutes(30)),
        new("Collect clothes", null, TimeSpan.FromHours(48), null),
    ];

    /// <summary>Adds a Chore with the "Wash clothes" Steps to a seeded Family.</summary>
    public static async Task<Guid> SeedChoreAsync(this IServiceProvider services, Guid familyId, string title = "Wash clothes")
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        using var _ = provider.GetRequiredService<TenantContext>().BeginScope(familyId);

        var chore = Chore.Create(Guid.CreateVersion7(), familyId, title, null, 1, WashClothesSteps());
        provider.GetRequiredService<IChoreRepository>().Add(chore);
        await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);
        return chore.Id;
    }
}
