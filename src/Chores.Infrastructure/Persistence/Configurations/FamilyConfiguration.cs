using Chores.Domain.Families;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chores.Infrastructure.Persistence.Configurations;

internal sealed class FamilyConfiguration : IEntityTypeConfiguration<Family>
{
    public void Configure(EntityTypeBuilder<Family> builder)
    {
        builder.ToTable("Families");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();
        builder.Property(f => f.Name).HasMaxLength(Family.NameMaxLength);
        builder.Property(f => f.TimeZone)
            .HasConversion(tz => tz.Id, id => FamilyTimeZone.Create(id))
            .HasMaxLength(64);
        builder.Property(f => f.Revision).IsConcurrencyToken();

        builder.HasMany(f => f.Members).WithOne().HasForeignKey(m => m.FamilyId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(f => f.Members).HasField("_members").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
