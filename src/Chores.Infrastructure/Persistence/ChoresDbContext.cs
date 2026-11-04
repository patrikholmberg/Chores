using System.Linq.Expressions;
using Chores.Application.Common.Tenancy;
using Chores.Domain.ChoreAttempts;
using Chores.Domain.ChoreLibrary;
using Chores.Domain.Common;
using Chores.Domain.Families;
using Chores.Infrastructure.Captcha;
using Chores.Infrastructure.Identity;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Chores.Infrastructure.Persistence;

/// <summary>
/// The single database context: domain data, Identity credentials, OpenIddict and Data Protection storage.
/// Family-owned data is filtered to <see cref="CurrentFamilyId"/> on read and guarded on save.
/// </summary>
public sealed class ChoresDbContext(DbContextOptions<ChoresDbContext> options, ITenantContext tenant)
    : IdentityUserContext<MemberCredential, Guid>(options), IDataProtectionKeyContext
{
    /// <summary>Name of the query filter that limits Family-owned data to the current Family.</summary>
    public const string TenantFilter = "Tenant";

    /// <summary>Name of the query filter that hides removed Members.</summary>
    public const string RemovedFilter = "Removed";

    public DbSet<Family> Families => Set<Family>();

    public DbSet<Member> Members => Set<Member>();

    public DbSet<Chore> Chores => Set<Chore>();

    public DbSet<ChoreAttempt> ChoreAttempts => Set<ChoreAttempt>();

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    internal DbSet<UsedCaptchaChallenge> UsedCaptchaChallenges => Set<UsedCaptchaChallenge>();

    /// <summary>Read by the query filters on every query, so EF Core parameterises it per context instance.</summary>
    public Guid? CurrentFamilyId => tenant.FamilyId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MemberCredential>(b =>
        {
            b.ToTable("MemberCredentials");
            b.HasOne<Member>().WithOne().HasForeignKey<MemberCredential>(c => c.Id).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<Guid>>().ToTable("MemberCredentialClaims");
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<Guid>>().ToTable("MemberCredentialLogins");
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<Guid>>().ToTable("MemberCredentialTokens");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChoresDbContext).Assembly);

        modelBuilder.Entity<Family>().HasQueryFilter(TenantFilter, f => f.Id == CurrentFamilyId);
        modelBuilder.Entity<Member>().HasQueryFilter(RemovedFilter, m => m.RemovedAt == null);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(IFamilyOwned).IsAssignableFrom(t.ClrType)))
        {
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(TenantFilter, BuildTenantFilter(entityType.ClrType));
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        GuardTenant();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        GuardTenant();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary><c>e =&gt; (Guid?)e.FamilyId == CurrentFamilyId</c> for an entity type known only at runtime.</summary>
    private LambdaExpression BuildTenantFilter(Type entityType)
    {
        var entity = Expression.Parameter(entityType, "e");
        var familyId = Expression.Convert(Expression.Property(entity, nameof(IFamilyOwned.FamilyId)), typeof(Guid?));
        var current = Expression.Property(Expression.Constant(this), nameof(CurrentFamilyId));
        return Expression.Lambda(Expression.Equal(familyId, current), entity);
    }

    /// <summary>
    /// New Family-owned rows always belong to the current Family; changing or deleting another Family's rows, or any
    /// Family-owned row without a current Family, is refused.
    /// </summary>
    private void GuardTenant()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            var familyIdProperty = entry.Entity switch
            {
                IFamilyOwned => entry.Property(nameof(IFamilyOwned.FamilyId)),
                Family => entry.Property(nameof(Family.Id)),
                _ => null,
            };
            if (familyIdProperty is null)
            {
                continue;
            }

            var current = CurrentFamilyId
                ?? throw new InvalidOperationException($"Cannot save {entry.Metadata.DisplayName()} without a current Family.");

            if (entry.State == EntityState.Added && entry.Entity is IFamilyOwned)
            {
                familyIdProperty.CurrentValue = current;
            }
            else if (!Equals(familyIdProperty.CurrentValue, current) || !Equals(familyIdProperty.OriginalValue, current))
            {
                throw new InvalidOperationException(
                    $"Cannot save {entry.Metadata.DisplayName()} of another Family than the current one.");
            }
        }
    }
}
