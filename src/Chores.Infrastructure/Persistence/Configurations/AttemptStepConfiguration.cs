using Chores.Domain.ChoreAttempts;
using Chores.Domain.ChoreLibrary;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chores.Infrastructure.Persistence.Configurations;

internal sealed class AttemptStepConfiguration : IEntityTypeConfiguration<AttemptStep>
{
    public void Configure(EntityTypeBuilder<AttemptStep> builder)
    {
        builder.ToTable("ChoreAttemptSteps");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Title).HasMaxLength(Step.TitleMaxLength);
        builder.Property(s => s.Description).HasMaxLength(Step.DescriptionMaxLength);
        builder.Property(s => s.ReminderDelay).HasColumnType("interval");
        builder.Property(s => s.RepeatInterval).HasColumnType("interval");
        builder.Ignore(s => s.IsDone);
        builder.HasIndex(s => s.FamilyId);
        builder.HasIndex(s => s.AttemptId);
    }
}
