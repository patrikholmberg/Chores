using Chores.Api.Tests.Infrastructure;
using Chores.Application.Common.Exceptions;
using Chores.Application.Common.Identity;
using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Domain.Families;
using Chores.Infrastructure.Identity;
using Chores.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;

namespace Chores.Api.Tests.Persistence;

public class PersistenceTests(PostgresApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Family_member_and_credential_are_saved_together()
    {
        var seeded = await factory.Services.SeedFamilyAsync();

        await using var scope = factory.Services.CreateAsyncScope();
        var directory = scope.ServiceProvider.GetRequiredService<MemberDirectory>();
        var credentials = scope.ServiceProvider.GetRequiredService<IMemberCredentials>();

        Assert.Equal(seeded.Id, (await directory.FindAsync(seeded.Child.Id, Ct))?.FamilyId);
        Assert.True(await credentials.CheckPasswordAsync(seeded.Child.Id, TestData.ChildPassword, Ct));
    }

    [Fact]
    public async Task Concurrent_use_of_one_username_saves_only_the_first_family()
    {
        var username = Username.Create(TestData.UniqueUsername());
        await using var first = factory.Services.CreateAsyncScope();
        await using var second = factory.Services.CreateAsyncScope();

        var firstFamily = await StageFamilyAsync(first.ServiceProvider, username);
        var secondFamily = await StageFamilyAsync(second.ServiceProvider, username);

        await first.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(Ct);
        await Assert.ThrowsAsync<UsernameTakenException>(() =>
            second.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(Ct));

        await using var check = factory.Services.CreateAsyncScope();
        var tenant = check.ServiceProvider.GetRequiredService<TenantContext>();
        var db = check.ServiceProvider.GetRequiredService<ChoresDbContext>();
        using (tenant.BeginScope(secondFamily))
        {
            Assert.False(await db.Families.AnyAsync(Ct));
        }

        using (tenant.BeginScope(firstFamily))
        {
            Assert.True(await db.Families.AnyAsync(Ct));
        }
    }

    [Fact]
    public async Task Setting_a_password_replaces_the_old_one_and_revoking_ends_issued_tokens()
    {
        var seeded = await factory.Services.SeedFamilyAsync();
        await using var scope = factory.Services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var tokens = provider.GetRequiredService<IOpenIddictTokenManager>();
        var token = await tokens.CreateAsync(new OpenIddictTokenDescriptor
        {
            Subject = seeded.Child.Id.ToString(),
            Type = OpenIddictConstants.TokenTypeHints.RefreshToken,
            Status = OpenIddictConstants.Statuses.Valid,
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(1),
        }, Ct);

        var credentials = provider.GetRequiredService<IMemberCredentials>();
        await credentials.SetPasswordAsync(seeded.Child.Id, "new-pw", Ct);
        await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(Ct);
        await credentials.RevokeSessionsAsync(seeded.Child.Id, Ct);

        await using var check = factory.Services.CreateAsyncScope();
        var checkCredentials = check.ServiceProvider.GetRequiredService<IMemberCredentials>();
        Assert.True(await checkCredentials.CheckPasswordAsync(seeded.Child.Id, "new-pw", Ct));
        Assert.False(await checkCredentials.CheckPasswordAsync(seeded.Child.Id, TestData.ChildPassword, Ct));

        var checkTokens = check.ServiceProvider.GetRequiredService<IOpenIddictTokenManager>();
        var reloaded = await checkTokens.FindByIdAsync((await tokens.GetIdAsync(token, Ct))!, Ct);
        Assert.Equal(OpenIddictConstants.Statuses.Revoked, await checkTokens.GetStatusAsync(reloaded!, Ct));
    }

    [Fact]
    public async Task Saving_a_family_owned_row_without_a_current_family_is_refused()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var family = Family.Register(
            Guid.CreateVersion7(), "No scope", FamilyTimeZone.Create("Europe/Stockholm"),
            Guid.CreateVersion7(), Username.Create(TestData.UniqueUsername()), null, "x@example.com", PreferredLanguage.English);
        scope.ServiceProvider.GetRequiredService<IFamilyRepository>().Add(family);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(Ct));
    }

    private static async Task<Guid> StageFamilyAsync(IServiceProvider provider, Username username)
    {
        var familyId = Guid.CreateVersion7();
        var parentId = Guid.CreateVersion7();
        provider.GetRequiredService<TenantContext>().BeginScope(familyId);
        provider.GetRequiredService<IFamilyRepository>().Add(Family.Register(
            familyId, "Race", FamilyTimeZone.Create("Europe/Stockholm"),
            parentId, username, null, "race@example.com", PreferredLanguage.English));
        await provider.GetRequiredService<IMemberCredentials>()
            .CreateAsync(parentId, username, "race@example.com", TestData.ParentPassword, CancellationToken.None);
        return familyId;
    }
}
