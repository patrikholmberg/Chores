using Chores.Domain.ChoreLibrary;
using Chores.Domain.Families;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chores.Infrastructure.Persistence.Configurations;

internal sealed class ChoreConfiguration : IEntityTypeConfiguration<Chore>
{
    public void Configure(EntityTypeBuilder<Chore> builder)
    {
        builder.ToTable("Chores");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Title).HasMaxLength(Chore.TitleMaxLength);
        builder.Property(c => c.Description).HasMaxLength(Chore.DescriptionMaxLength);
        builder.Property(c => c.Revision).IsConcurrencyToken();
        builder.Ignore(c => c.IsArchived);
        builder.HasIndex(c => c.FamilyId);
        builder.HasOne<Family>().WithMany().HasForeignKey(c => c.FamilyId).OnDelete(DeleteBehavior.Restrict);

        // Steps dropped from the list by Chore.Revise are deleted as orphans.
        builder.HasMany(c => c.Steps).WithOne().HasForeignKey(s => s.ChoreId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.Steps).HasField("_steps").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
