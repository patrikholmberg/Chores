using Chores.Application.Common.Tenancy;
using Chores.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chores.Architecture.Tests;

public class TenantIsolationTests
{
    /// <summary>The only file allowed to call <c>IgnoreQueryFilters</c>.</summary>
    private const string AllowedBypass = "src/Chores.Infrastructure/Identity/MemberDirectory.cs";

    [Fact]
    public void Every_entity_with_a_FamilyId_has_the_tenant_filter()
    {
        using var db = CreateContext();

        var unfiltered = db.Model.GetEntityTypes()
            .Where(t => t.FindProperty("FamilyId") is not null)
            .Where(t => t.FindDeclaredQueryFilter(ChoresDbContext.TenantFilter) is null)
            .Select(t => t.DisplayName())
            .ToList();

        Assert.True(unfiltered.Count == 0, "Family-owned entities without tenant filter: " + string.Join(", ", unfiltered));
    }

    [Fact]
    public void The_family_itself_has_the_tenant_filter()
    {
        using var db = CreateContext();

        Assert.NotNull(db.Model.FindEntityType(typeof(Domain.Families.Family))!.FindDeclaredQueryFilter(ChoresDbContext.TenantFilter));
    }

    [Fact]
    public void Query_filters_are_only_bypassed_in_the_member_directory()
    {
        var root = FindRepositoryRoot();

        var offenders = Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(f => File.ReadAllText(f).Contains("IgnoreQueryFilters"))
            .Select(f => Path.GetRelativePath(root, f).Replace(Path.DirectorySeparatorChar, '/'))
            .Where(f => f != AllowedBypass)
            .ToList();

        Assert.True(offenders.Count == 0, "IgnoreQueryFilters used outside MemberDirectory: " + string.Join(", ", offenders));
    }

    private static ChoresDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ChoresDbContext>();
        options.UseNpgsql("Host=localhost;Database=model-only").UseOpenIddict<Guid>();
        return new ChoresDbContext(options.Options, new FixedTenant());
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Chores.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    private sealed class FixedTenant : ITenantContext
    {
        public Guid? FamilyId => Guid.Empty;
    }
}
