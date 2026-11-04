using Chores.Domain.ChoreAttempts;
using Chores.Domain.ChoreLibrary;
using Chores.Domain.Families;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chores.Infrastructure.Persistence.Configurations;

internal sealed class ChoreAttemptConfiguration : IEntityTypeConfiguration<ChoreAttempt>
{
    public void Configure(EntityTypeBuilder<ChoreAttempt> builder)
    {
        builder.ToTable("ChoreAttempts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Title).HasMaxLength(Chore.TitleMaxLength);
        builder.Property(a => a.Description).HasMaxLength(Chore.DescriptionMaxLength);
        builder.Property(a => a.RejectionReason).HasMaxLength(ChoreAttempt.RejectionReasonMaxLength);
        builder.Property(a => a.StartDay).HasColumnType("date");
        builder.Property(a => a.Revision).IsConcurrencyToken();
        builder.Ignore(a => a.CurrentStep);
        builder.HasIndex(a => a.FamilyId);
        builder.HasIndex(a => new { a.FamilyId, a.ChildId, a.StartDay });
        builder.HasOne<Family>().WithMany().HasForeignKey(a => a.FamilyId).OnDelete(DeleteBehavior.Restrict);

        // Members are only ever soft-removed. There is deliberately no foreign key to Chores: the snapshot outlives the Chore.
        builder.HasOne<Member>().WithMany().HasForeignKey(a => a.ChildId).OnDelete(DeleteBehavior.Restrict);

        // Cancelling an attempt deletes it together with its Steps.
        builder.HasMany(a => a.Steps).WithOne().HasForeignKey(s => s.AttemptId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(a => a.Steps).HasField("_steps").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
