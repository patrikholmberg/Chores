using Chores.Domain.ChoreLibrary;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chores.Infrastructure.Persistence.Configurations;

internal sealed class StepConfiguration : IEntityTypeConfiguration<Step>
{
    public void Configure(EntityTypeBuilder<Step> builder)
    {
        builder.ToTable("ChoreSteps");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Title).HasMaxLength(Step.TitleMaxLength);
        builder.Property(s => s.Description).HasMaxLength(Step.DescriptionMaxLength);
        builder.Property(s => s.ReminderDelay).HasColumnType("interval");
        builder.Property(s => s.RepeatInterval).HasColumnType("interval");
        builder.HasIndex(s => s.FamilyId);
        builder.HasIndex(s => s.ChoreId);
    }
}
